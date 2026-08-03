using System.ComponentModel;
using Bee.Base.Security;
using Bee.Business.AuditLog;
using Bee.Business.System;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// 部署層作業（<see cref="SystemBusinessObject.SetDeploymentAdmin"/> /
    /// <see cref="SystemBusinessObject.CreateApiKey"/>）的稽核留痕：寫進變更軸、標為敏感、
    /// 帶得出前後值，且金鑰的祕密段與雜湊絕不進日誌。
    /// </summary>
    public class SystemBusinessObjectDeploymentAuditTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectDeploymentAuditTests(SharedDbFixture fx) { _fx = fx; }

        private IDbConnectionManager ConnectionManager => _fx.GetRequiredService<IDbConnectionManager>();

        /// <summary>
        /// 建立一個 BO，稽核寫入端換成捕捉用的假實作。
        /// </summary>
        /// <param name="writer">捕捉到的稽核項目。</param>
        /// <param name="enabled">全域稽核開關。</param>
        /// <param name="changeEnabled">資料變更類別開關——部署層作業刻意不受它影響。</param>
        private SystemBusinessObject CreateBo(out CapturingAuditLogWriter writer,
            bool enabled = true, bool changeEnabled = true)
        {
            writer = new CapturingAuditLogWriter();
            var ctx = TestBeeContext.CreateWithOverrides(_fx,
                (typeof(AuditLogOptions), new AuditLogOptions { Enabled = enabled, ChangeEnabled = changeEnabled }),
                (typeof(IAuditLogWriter), writer));
            return new SystemBusinessObject(ctx, Guid.Empty);
        }

        private static ChangeAuditEntry SingleChange(CapturingAuditLogWriter writer)
        {
            var entry = Assert.Single(writer.Entries);
            return Assert.IsType<ChangeAuditEntry>(entry);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SetDeploymentAdmin 應留下標為敏感的稽核，且帶得出 false → true 的方向")]
        public void SetDeploymentAdmin_Grant_WritesSensitiveAuditWithDirection()
        {
            string userId = TestUsers.Create(ConnectionManager, "audit-grant");
            try
            {
                var bo = CreateBo(out var writer);

                bo.SetDeploymentAdmin(new SetDeploymentAdminArgs { UserId = userId, IsDeploymentAdmin = true });

                var entry = SingleChange(writer);
                Assert.Equal(SysProgIds.System, entry.ProgId);
                Assert.Equal("st_user", entry.ChangeTableName);
                Assert.Equal(ChangeKind.Update, entry.ChangeKind);
                // 提權動作一律標敏感：篩掉雜訊時不該連它一起篩掉。
                Assert.True(entry.IsSensitive);
                Assert.Equal("System.SetDeploymentAdmin", entry.Source);

                // 授予與撤銷同為 Update，沒有前後值就分不出方向。
                var field = Assert.Single(ChangeDiffGramReader.Read(entry.ChangesXml));
                Assert.Equal(ProtectedFields.DeploymentAdmin, field.FieldName);
                Assert.Equal("False", field.OldValue);
                Assert.Equal("True", field.NewValue);
            }
            finally
            {
                TestUsers.Delete(ConnectionManager, userId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SetDeploymentAdmin 撤銷時稽核應帶得出 true → false 的方向")]
        public void SetDeploymentAdmin_Revoke_WritesOppositeDirection()
        {
            string userId = TestUsers.Create(ConnectionManager, "audit-revoke");
            try
            {
                var granting = CreateBo(out _);
                granting.SetDeploymentAdmin(new SetDeploymentAdminArgs { UserId = userId, IsDeploymentAdmin = true });

                var bo = CreateBo(out var writer);
                bo.SetDeploymentAdmin(new SetDeploymentAdminArgs { UserId = userId, IsDeploymentAdmin = false });

                var field = Assert.Single(ChangeDiffGramReader.Read(SingleChange(writer).ChangesXml));
                Assert.Equal("True", field.OldValue);
                Assert.Equal("False", field.NewValue);
            }
            finally
            {
                TestUsers.Delete(ConnectionManager, userId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("CreateApiKey 應留下稽核，且明文祕密與雜湊都不進日誌")]
        public void CreateApiKey_WritesAuditWithoutSecretOrHash()
        {
            string sysId = "audit-" + Guid.NewGuid().ToString("N");
            try
            {
                var bo = CreateBo(out var writer);

                var result = bo.CreateApiKey(new CreateApiKeyArgs { SysId = sysId, SysName = "Audited app" });

                var entry = SingleChange(writer);
                Assert.Equal(SysProgIds.System, entry.ProgId);
                Assert.Equal("st_api_key", entry.ChangeTableName);
                Assert.Equal(ChangeKind.Insert, entry.ChangeKind);
                Assert.Equal(sysId, entry.RowKey);
                Assert.Equal("System.CreateApiKey", entry.Source);

                var fields = ChangeDiffGramReader.Read(entry.ChangesXml);
                Assert.Contains(fields, f => f.FieldName == SysFields.Id && f.NewValue == sysId);
                Assert.Contains(fields, f => f.FieldName == SysFields.Name && f.NewValue == "Audited app");

                // 稽核列的讀者與 st_api_key 的讀者不是同一群；祕密段連雜湊都不該落到這裡。
                Assert.True(ApiKeyFormat.TryParse(result.ApiKey, out _, out string secret));
                Assert.DoesNotContain(secret, entry.ChangesXml, StringComparison.Ordinal);
                Assert.DoesNotContain("hashed_key", entry.ChangesXml, StringComparison.Ordinal);
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("關閉資料變更稽核不影響部署層作業的留痕")]
        public void SetDeploymentAdmin_ChangeAuditDisabled_StillWrites()
        {
            string userId = TestUsers.Create(ConnectionManager, "audit-chgoff");
            try
            {
                // ChangeEnabled 是給「業務資料歷程量太大」用的開關，關掉它不該連提權也一起靜音。
                var bo = CreateBo(out var writer, enabled: true, changeEnabled: false);

                bo.SetDeploymentAdmin(new SetDeploymentAdminArgs { UserId = userId, IsDeploymentAdmin = true });

                Assert.Single(writer.Entries);
            }
            finally
            {
                TestUsers.Delete(ConnectionManager, userId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("關閉全域稽核時部署層作業不留痕")]
        public void SetDeploymentAdmin_AuditDisabled_WritesNothing()
        {
            string userId = TestUsers.Create(ConnectionManager, "audit-off");
            try
            {
                var bo = CreateBo(out var writer, enabled: false);

                bo.SetDeploymentAdmin(new SetDeploymentAdminArgs { UserId = userId, IsDeploymentAdmin = true });

                Assert.Empty(writer.Entries);
            }
            finally
            {
                TestUsers.Delete(ConnectionManager, userId);
            }
        }

        private void DeleteKey(string sysId)
        {
            var dbType = ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string sql = $"DELETE FROM {dbType.QuoteIdentifier("st_api_key")} " +
                         $"WHERE {dbType.QuoteIdentifier("sys_id")} = {{0}}";
            new DbAccess(DbCategoryIds.Common, ConnectionManager)
                .Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, sysId));
        }

        private sealed class CapturingAuditLogWriter : IAuditLogWriter
        {
            public List<AuditEntry> Entries { get; } = [];

            public void Write(AuditEntry entry) => Entries.Add(entry);
        }
    }
}
