using System.ComponentModel;
using Bee.Business.System;
using Bee.Db;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject.EnterCompany"/> 行為測試。透過 seed user '001' 與
    /// seed company 'C001' 走真實 DB 對照路徑；其他情境用 SQL helper 動態建 company / 對照
    /// 並於 finally 清理。
    /// </summary>
    public class SystemBusinessObjectEnterCompanyTests : IClassFixture<SharedDbFixture>
    {
        private const string SeedUserId = "001";
        private const string SeedCompanyId = "C001";
        // BO 測試綁 SQL Server；company 的 permission 表（st_role_grant / st_user_role）位於
        // company-category DB，故 company_database_id 須指向該庫，EnterCompany 才載得到角色快照。
        private static readonly string CompanyDbId = TestDbConventions.GetDatabaseId(DatabaseType.SQLServer, "company");
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectEnterCompanyTests(SharedDbFixture fx) { _fx = fx; }

        #region Helpers (SQL Server only — BO tests bind to `common` databaseId which points to SQL Server)

        private DbAccess Common() => _fx.NewDbAccess("common");

        private Guid InsertCompany(string companyId, bool enabled)
        {
            var rowId = Guid.NewGuid();
            string enabledLiteral = enabled ? "1" : "0";
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO st_company (sys_rowid, sys_id, sys_name, company_database_id, enabled, sys_insert_time) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {enabledLiteral}, GETDATE())",
                rowId, companyId, "BO 測試公司", CompanyDbId);
            Common().Execute(insert);
            return rowId;
        }

        private Guid InsertCompanyWithCustomize(string companyId, string customizeId)
        {
            var rowId = Guid.NewGuid();
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO st_company (sys_rowid, sys_id, sys_name, company_database_id, customize_id, enabled, sys_insert_time) " +
                "VALUES ({0}, {1}, {2}, {3}, {4}, 1, GETDATE())",
                rowId, companyId, "BO 客製測試公司", CompanyDbId, customizeId);
            Common().Execute(insert);
            return rowId;
        }

        private Guid InsertGrant(Guid userRowId, Guid companyRowId)
        {
            var rowId = Guid.NewGuid();
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO st_user_company (sys_rowid, user_rowid, company_rowid, sys_insert_time) " +
                "VALUES ({0}, {1}, {2}, GETDATE())",
                rowId, userRowId, companyRowId);
            Common().Execute(insert);
            return rowId;
        }

        private void DeleteCompany(Guid companyRowId)
        {
            var delete = new DbCommandSpec(DbCommandKind.NonQuery,
                "DELETE FROM st_company WHERE sys_rowid = {0}", companyRowId);
            Common().Execute(delete);
        }

        private void DeleteGrant(Guid grantRowId)
        {
            var delete = new DbCommandSpec(DbCommandKind.NonQuery,
                "DELETE FROM st_user_company WHERE sys_rowid = {0}", grantRowId);
            Common().Execute(delete);
        }

        private Guid LookupUserRowId(string userId)
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT sys_rowid FROM st_user WHERE sys_id = {0}", userId);
            var result = Common().Execute(spec);
            var value = result.Scalar;
            if (value is Guid g) return g;
            if (value is byte[] b && b.Length == 16) return new Guid(b);
            if (value is string s && Guid.TryParse(s, out var parsed)) return parsed;
            throw new InvalidOperationException($"Cannot resolve user rowid for '{userId}'.");
        }

        // ft_employee lives in the company database (CompanyDbId), not common.
        private DbAccess CompanyDb() => _fx.NewDbAccess(CompanyDbId);

        private void InsertEmployee(Guid empRowId, string empId, Guid deptRowId, Guid userRowId)
        {
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO ft_employee (sys_rowid, sys_id, sys_name, dept_rowid, user_rowid) " +
                "VALUES ({0}, {1}, {2}, {3}, {4})",
                empRowId, empId, "BO 測試員工", deptRowId, userRowId);
            CompanyDb().Execute(insert);
        }

        private void DeleteEmployee(Guid empRowId)
        {
            var delete = new DbCommandSpec(DbCommandKind.NonQuery,
                "DELETE FROM ft_employee WHERE sys_rowid = {0}", empRowId);
            CompanyDb().Execute(delete);
        }

        #endregion

        [Fact]
        [DisplayName("EnterCompany seed 對照存在時應回傳 CompanyInfo 並設定 SessionInfo.CompanyId")]
        public void EnterCompany_ValidCompany_BindsAndReturns()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                var result = bo.EnterCompany(new EnterCompanyArgs { CompanyId = SeedCompanyId });

                Assert.NotNull(result);
                Assert.Equal(SeedCompanyId, result.Company.CompanyId);

                var session = sessionService.Get(accessToken);
                Assert.NotNull(session);
                Assert.Equal(SeedCompanyId, session.CompanyId);
                // Seed company 'C001' ships no customization → standard (empty) code.
                Assert.Equal(string.Empty, session.CustomizeId);
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 公司有 customize_id 時應寫入 SessionInfo.CustomizeId；LeaveCompany 應清空")]
        public void EnterCompany_CustomizedCompany_SetsThenClearsSessionCustomizeId()
        {
            const string customizeId = "ACME";
            var companyId = "CUST_" + Guid.NewGuid().ToString("N")[..6];
            var companyRowId = InsertCompanyWithCustomize(companyId, customizeId);
            var userRowId = LookupUserRowId(SeedUserId);
            var grantRowId = InsertGrant(userRowId, companyRowId);
            try
            {
                var sessionService = _fx.GetRequiredService<ISessionInfoService>();
                var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
                var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

                try
                {
                    var result = bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyId });
                    Assert.Equal(customizeId, result.Company.CustomizeId);
                    Assert.Equal(customizeId, sessionService.Get(accessToken)!.CustomizeId);

                    bo.LeaveCompany(new LeaveCompanyArgs());
                    Assert.Equal(string.Empty, sessionService.Get(accessToken)!.CustomizeId);
                }
                finally
                {
                    sessionService.Remove(accessToken);
                }
            }
            finally
            {
                DeleteGrant(grantRowId);
                DeleteCompany(companyRowId);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 應解析並快照 user/employee/dept rowid；LeaveCompany 應清空")]
        public void EnterCompany_SnapshotsEmployeeContext_ThenClears()
        {
            var userRowId = LookupUserRowId(SeedUserId);
            var empRowId = Guid.NewGuid();
            var deptRowId = Guid.NewGuid();
            var empId = "EMP_" + Guid.NewGuid().ToString("N")[..6];
            InsertEmployee(empRowId, empId, deptRowId, userRowId);
            try
            {
                var sessionService = _fx.GetRequiredService<ISessionInfoService>();
                var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
                var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

                try
                {
                    bo.EnterCompany(new EnterCompanyArgs { CompanyId = SeedCompanyId });

                    var session = sessionService.Get(accessToken)!;
                    Assert.Equal(userRowId, session.UserRowId);
                    Assert.Equal(empRowId, session.EmployeeRowId);
                    Assert.Equal(deptRowId, session.DeptRowId);

                    bo.LeaveCompany(new LeaveCompanyArgs());
                    var cleared = sessionService.Get(accessToken)!;
                    Assert.Equal(Guid.Empty, cleared.UserRowId);
                    Assert.Equal(Guid.Empty, cleared.EmployeeRowId);
                    Assert.Equal(Guid.Empty, cleared.DeptRowId);
                }
                finally
                {
                    sessionService.Remove(accessToken);
                }
            }
            finally
            {
                DeleteEmployee(empRowId);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 無對應員工時 user rowid 仍快照、employee/dept 為空")]
        public void EnterCompany_NoEmployee_SnapshotsUserRowIdOnly()
        {
            var userRowId = LookupUserRowId(SeedUserId);
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                bo.EnterCompany(new EnterCompanyArgs { CompanyId = SeedCompanyId });

                // Seed user '001' has no ft_employee row → user rowid resolves, employee/dept empty.
                var session = sessionService.Get(accessToken)!;
                Assert.Equal(userRowId, session.UserRowId);
                Assert.Equal(Guid.Empty, session.EmployeeRowId);
                Assert.Equal(Guid.Empty, session.DeptRowId);
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 不存在的 CompanyId 應拋 Company access denied")]
        public void EnterCompany_UnknownCompany_ThrowsAccessDenied()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);
            var unknown = "UNK_" + Guid.NewGuid().ToString("N")[..6];

            try
            {
                var ex = Assert.Throws<InvalidOperationException>(
                    () => bo.EnterCompany(new EnterCompanyArgs { CompanyId = unknown }));
                Assert.Contains("Company access denied", ex.Message);

                var session = sessionService.Get(accessToken);
                Assert.NotNull(session);
                Assert.Null(session.CompanyId);
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 公司存在但 user 沒被 grant 應拋 Company access denied")]
        public void EnterCompany_NoAccess_ThrowsAccessDenied()
        {
            var companyId = "NOGRANT_" + Guid.NewGuid().ToString("N")[..6];
            var companyRowId = InsertCompany(companyId, enabled: true);
            try
            {
                var sessionService = _fx.GetRequiredService<ISessionInfoService>();
                var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
                var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

                try
                {
                    var ex = Assert.Throws<InvalidOperationException>(
                        () => bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyId }));
                    Assert.Contains("Company access denied", ex.Message);
                }
                finally
                {
                    sessionService.Remove(accessToken);
                }
            }
            finally
            {
                DeleteCompany(companyRowId);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 公司停用但 user 已 grant 應拋 Company access denied")]
        public void EnterCompany_DisabledCompany_ThrowsAccessDenied()
        {
            var companyId = "DIS_" + Guid.NewGuid().ToString("N")[..6];
            var companyRowId = InsertCompany(companyId, enabled: false);
            var userRowId = LookupUserRowId(SeedUserId);
            var grantRowId = InsertGrant(userRowId, companyRowId);
            try
            {
                var sessionService = _fx.GetRequiredService<ISessionInfoService>();
                var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
                var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

                try
                {
                    var ex = Assert.Throws<InvalidOperationException>(
                        () => bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyId }));
                    Assert.Contains("Company access denied", ex.Message);
                }
                finally
                {
                    sessionService.Remove(accessToken);
                }
            }
            finally
            {
                DeleteGrant(grantRowId);
                DeleteCompany(companyRowId);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 切換到另一已 grant 的 company 應覆寫 SessionInfo.CompanyId")]
        public void EnterCompany_SwitchToAnotherCompany_Overwrites()
        {
            var companyB = "ALT_" + Guid.NewGuid().ToString("N")[..6];
            var companyBRowId = InsertCompany(companyB, enabled: true);
            var userRowId = LookupUserRowId(SeedUserId);
            var grantRowId = InsertGrant(userRowId, companyBRowId);
            try
            {
                var sessionService = _fx.GetRequiredService<ISessionInfoService>();
                var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
                var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

                try
                {
                    bo.EnterCompany(new EnterCompanyArgs { CompanyId = SeedCompanyId });
                    Assert.Equal(SeedCompanyId, sessionService.Get(accessToken)!.CompanyId);

                    bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyB });
                    Assert.Equal(companyB, sessionService.Get(accessToken)!.CompanyId);
                }
                finally
                {
                    sessionService.Remove(accessToken);
                }
            }
            finally
            {
                DeleteGrant(grantRowId);
                DeleteCompany(companyBRowId);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 對同一 CompanyId 重複呼叫應 idempotent")]
        public void EnterCompany_SameCompany_Idempotent()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                bo.EnterCompany(new EnterCompanyArgs { CompanyId = SeedCompanyId });
                bo.EnterCompany(new EnterCompanyArgs { CompanyId = SeedCompanyId });
                Assert.Equal(SeedCompanyId, sessionService.Get(accessToken)!.CompanyId);
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 對空 CompanyId 應拋 ArgumentException")]
        public void EnterCompany_EmptyCompanyId_ThrowsArgumentException()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();

            try
            {
                Assert.Throws<ArgumentException>(
                    () => bo.EnterCompany(new EnterCompanyArgs { CompanyId = string.Empty }));
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 對 null args 應拋 ArgumentNullException")]
        public void EnterCompany_NullArgs_ThrowsArgumentNullException()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: SeedUserId);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();

            try
            {
                Assert.Throws<ArgumentNullException>(() => bo.EnterCompany(null!));
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }
    }
}
