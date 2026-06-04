using System.ComponentModel;
using Bee.Business.System;
using Bee.Business.UnitTests.Fakes;
using Bee.Db;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// SystemBO session lifecycle 全流程整合測試。串接 Login → EnterCompany(A) →
    /// EnterCompany(B) → LeaveCompany → EnterCompany(A) → Logout，驗證跨四個方法的
    /// session state transition 一致性，並涵蓋 plan-system-bo-session-lifecycle.md
    /// 列出的合法 / 非法路徑。
    /// </summary>
    public class SystemBusinessObjectLifecycleTests : IClassFixture<SharedDbFixture>
    {
        // company 的 permission 表位於 company-category DB；company_database_id 須指向該庫，
        // EnterCompany 才載得到角色快照。BO 測試綁 SQL Server。
        private static readonly string CompanyDbId = TestDbConventions.GetDatabaseId(DatabaseType.SQLServer, "company");
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectLifecycleTests(SharedDbFixture fx) { _fx = fx; }

        private static string UniqueCompanyId() => "C_" + Guid.NewGuid().ToString("N")[..12];

        [Fact]
        [DisplayName("Login → EnterCompany(A) → EnterCompany(B) → LeaveCompany → EnterCompany(A) → Logout 整條 session lifecycle 應一致")]
        public void FullLifecycle_LoginThroughLogout_TransitionsCorrectly()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            // companyA 用 seed C001（已有 user '001' 對照）；companyB 動態建立 + grant，
            // 走真實 st_company / st_user_company 路徑符合新加入的 HasAccess 驗證。
            const string companyA = "C001";
            var companyB = UniqueCompanyId();
            var (companyBRowId, grantBRowId) = InsertCompanyAndGrantForSeedUser(companyB);

            try
            {
                // 1. Login — 用 TestableSystemBusinessObject 繞過預設的 AuthenticateUser=false；
                // user id 必須對應 seed user '001'，否則 HasAccess JOIN 找不到對照。
                var loginBo = new TestableSystemBusinessObject(
                    TestBeeContext.Create(_fx),
                    Guid.Empty,
                    _ => (true, "Integration User"));
                var loginResult = loginBo.Login(new LoginArgs { UserId = "001", Password = "pwd" });
                Assert.NotEqual(Guid.Empty, loginResult.AccessToken);
                var accessToken = loginResult.AccessToken;
                Assert.Null(sessionService.Get(accessToken)!.CompanyId);

                // 後續方法用一般 SystemBusinessObject + Login 取得的 AccessToken
                var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

                // 2. EnterCompany(A) — 首次進公司
                var enterA = bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyA });
                Assert.Equal(companyA, enterA.Company.CompanyId);
                Assert.Equal(companyA, sessionService.Get(accessToken)!.CompanyId);

                // 3. EnterCompany(B) — 切換（直接覆寫）
                var enterB = bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyB });
                Assert.Equal(companyB, enterB.Company.CompanyId);
                Assert.Equal(companyB, sessionService.Get(accessToken)!.CompanyId);

                // 4. LeaveCompany — 清回未進公司狀態
                bo.LeaveCompany(new LeaveCompanyArgs());
                Assert.Null(sessionService.Get(accessToken)!.CompanyId);

                // 5. EnterCompany(A) again — Leave 後重新進，狀態無痕殘留
                var enterAAgain = bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyA });
                Assert.Equal(companyA, enterAAgain.Company.CompanyId);
                Assert.Equal(companyA, sessionService.Get(accessToken)!.CompanyId);

                // 6. Logout — 隱含 LeaveCompany 清理，整個 session 從快取消失
                bo.Logout(new LogoutArgs());
                Assert.Null(sessionService.Get(accessToken));
            }
            finally
            {
                DeleteGrantAndCompany(grantBRowId, companyBRowId);
            }
        }

        // BO 整合測試僅綁定 `common` databaseId（SQL Server）；helper 寫 SQL Server 方言即可。
        private (Guid companyRowId, Guid grantRowId) InsertCompanyAndGrantForSeedUser(string companyId)
        {
            var dbAccess = _fx.NewDbAccess("common");
            var companyRowId = Guid.NewGuid();
            var grantRowId = Guid.NewGuid();

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO st_company (sys_rowid, sys_id, sys_name, company_database_id, enabled, sys_insert_time) " +
                "VALUES ({0}, {1}, {2}, {3}, 1, GETDATE())",
                companyRowId, companyId, "Lifecycle B", CompanyDbId));

            var userLookup = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT sys_rowid FROM st_user WHERE sys_id = {0}", "001"));
            var userRowId = (Guid)userLookup.Scalar!;

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO st_user_company (sys_rowid, user_rowid, company_rowid, sys_insert_time) " +
                "VALUES ({0}, {1}, {2}, GETDATE())",
                grantRowId, userRowId, companyRowId));

            return (companyRowId, grantRowId);
        }

        private void DeleteGrantAndCompany(Guid grantRowId, Guid companyRowId)
        {
            var dbAccess = _fx.NewDbAccess("common");
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                "DELETE FROM st_user_company WHERE sys_rowid = {0}", grantRowId));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                "DELETE FROM st_company WHERE sys_rowid = {0}", companyRowId));
        }

        [Fact]
        [DisplayName("Logout 後再呼叫 EnterCompany 應拋 UnauthorizedAccessException")]
        public void AfterLogout_EnterCompany_ThrowsUnauthorized()
        {
            var companyService = _fx.GetRequiredService<ICompanyInfoService>();
            var companyId = UniqueCompanyId();
            companyService.Set(new CompanyInfo { CompanyId = companyId, CompanyName = "Acme" });

            try
            {
                var loginBo = new TestableSystemBusinessObject(
                    TestBeeContext.Create(_fx), Guid.Empty,
                    _ => (true, "User"));
                var accessToken = loginBo.Login(new LoginArgs { UserId = "u", Password = "p" }).AccessToken;
                var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);
                bo.Logout(new LogoutArgs());

                Assert.Throws<UnauthorizedAccessException>(
                    () => bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyId }));
            }
            finally
            {
                companyService.Remove(companyId);
            }
        }

        [Fact]
        [DisplayName("Login 後直接 Logout（未進公司）應 idempotent 通過")]
        public void Login_DirectLogout_WithoutEnteringCompany_Succeeds()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var loginBo = new TestableSystemBusinessObject(
                TestBeeContext.Create(_fx), Guid.Empty,
                _ => (true, "User"));
            var accessToken = loginBo.Login(new LoginArgs { UserId = "u", Password = "p" }).AccessToken;
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            bo.Logout(new LogoutArgs());

            Assert.Null(sessionService.Get(accessToken));
        }

        [Fact]
        [DisplayName("Login 後 LeaveCompany（未進公司）應 idempotent；SessionInfo.CompanyId 維持 null")]
        public void Login_LeaveCompanyWithoutEntering_Idempotent()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var loginBo = new TestableSystemBusinessObject(
                TestBeeContext.Create(_fx), Guid.Empty,
                _ => (true, "User"));
            var accessToken = loginBo.Login(new LoginArgs { UserId = "u", Password = "p" }).AccessToken;
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                bo.LeaveCompany(new LeaveCompanyArgs());
                Assert.Null(sessionService.Get(accessToken)!.CompanyId);
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }
    }
}
