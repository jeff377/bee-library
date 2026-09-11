using System.ComponentModel;
using Bee.Base.Security;
using Bee.Business.AuditLog;
using Bee.Business.System;
using Bee.Business.UnitTests.Fakes;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Tests.Shared;
using Microsoft.Extensions.Logging;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// 部署層作業（<see cref="SystemBusinessObject.SetDeploymentAdmin"/> /
    /// <see cref="SystemBusinessObject.CreateApiKey"/>）的稽核留痕：寫進變更軸、標為敏感、
    /// 帶得出前後值，且金鑰的祕密段與雜湊絕不進日誌。
    /// </summary>
    /// <remarks>
    /// 這些 BO 走 <c>DbScope.Common</c>，測試 fixture 把 <c>common</c> 綁在 SQL Server，
    /// 因此閘門必須是 <c>SQLServer</c>：先前標成 <c>SQLite</c> 時，跳過與否看的是
    /// <c>BEE_TEST_CONNSTR_SQLITE</c>，實際跑的卻是 SQL Server。
    /// </remarks>
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
            return CreateBo(writer, loggers: null, enabled, changeEnabled);
        }

        /// <summary>
        /// 建立一個 BO，稽核寫入端與 logger 由呼叫端指定。
        /// </summary>
        private SystemBusinessObject CreateBo(IAuditLogWriter writer, ILoggerFactory? loggers,
            bool enabled = true, bool changeEnabled = true)
        {
            var ctx = TestBeeContext.CreateWithOverrides(_fx,
                (typeof(AuditLogOptions), new AuditLogOptions { Enabled = enabled, ChangeEnabled = changeEnabled }),
                (typeof(IAuditLogWriter), writer),
                (typeof(ILoggerFactory), loggers));
            return new SystemBusinessObject(ctx, Guid.Empty, SysProgIds.System, isLocalCall: true);
        }

        private static ChangeAuditEntry SingleChange(CapturingAuditLogWriter writer)
        {
            var entry = Assert.Single(writer.Entries);
            return Assert.IsType<ChangeAuditEntry>(entry);
        }

        [DbFact(DatabaseType.SQLServer)]
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

        [DbFact(DatabaseType.SQLServer)]
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

        [DbFact(DatabaseType.SQLServer)]
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

        [DbFact(DatabaseType.SQLServer)]
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

        [DbFact(DatabaseType.SQLServer)]
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

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("應用程式名稱含 XML 不允許的控制字元時 CreateApiKey 仍成功，且稽核讀得回原值")]
        public void CreateApiKey_NameWithControlCharacter_WritesReadableAudit()
        {
            string sysId = "audit-" + Guid.NewGuid().ToString("N");
            const string name = "Pasted\u0001app";
            try
            {
                var bo = CreateBo(out var writer);

                var result = bo.CreateApiKey(new CreateApiKeyArgs { SysId = sysId, SysName = name });

                Assert.False(string.IsNullOrEmpty(result.ApiKey));
                var fields = ChangeDiffGramReader.Read(SingleChange(writer).ChangesXml);
                Assert.Contains(fields, f => f.FieldName == SysFields.Name && f.NewValue == name);
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("★稽核寫入失敗時 CreateApiKey 仍須交回金鑰——金鑰已寫入，拿不到祕密段就等於作廢——並記下錯誤 log")]
        public void CreateApiKey_AuditWriteFails_StillReturnsKeyAndLogsError()
        {
            string sysId = "audit-" + Guid.NewGuid().ToString("N");
            var loggers = new RecordingLoggerFactory();
            try
            {
                var bo = CreateBo(new ThrowingAuditLogWriter(), loggers);

                var result = bo.CreateApiKey(new CreateApiKeyArgs { SysId = sysId, SysName = "Audit sink down" });

                Assert.True(ApiKeyFormat.TryParse(result.ApiKey, out string parsedId, out _));
                Assert.Equal(sysId, parsedId);
                var error = Assert.Single(loggers.Entries, e => e.Level == LogLevel.Error);
                Assert.IsType<InvalidOperationException>(error.Exception);
                Assert.Contains(sysId, error.Message, StringComparison.Ordinal);
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("稽核寫入失敗時 SetDeploymentAdmin 的變更仍生效，並記下錯誤 log")]
        public void SetDeploymentAdmin_AuditWriteFails_StillAppliesAndLogsError()
        {
            string userId = TestUsers.Create(ConnectionManager, "audit-sinkdown");
            var loggers = new RecordingLoggerFactory();
            try
            {
                var bo = CreateBo(new ThrowingAuditLogWriter(), loggers);

                var result = bo.SetDeploymentAdmin(new SetDeploymentAdminArgs { UserId = userId, IsDeploymentAdmin = true });

                Assert.True(result.IsDeploymentAdmin);
                var error = Assert.Single(loggers.Entries, e => e.Level == LogLevel.Error);
                Assert.IsType<InvalidOperationException>(error.Exception);
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

        private sealed class ThrowingAuditLogWriter : IAuditLogWriter
        {
            public void Write(AuditEntry entry) => throw new InvalidOperationException("Audit sink unavailable.");
        }
    }
}
