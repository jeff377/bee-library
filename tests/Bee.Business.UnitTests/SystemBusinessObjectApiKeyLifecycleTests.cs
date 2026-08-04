using System.ComponentModel;
using Bee.Base.Exceptions;
using Bee.Business.AuditLog;
using Bee.Business.System;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;

using Bee.Definition;
namespace Bee.Business.UnitTests
{
    /// <summary>
    /// 金鑰生命週期（<see cref="SystemBusinessObject.ListApiKeys"/> /
    /// <see cref="SystemBusinessObject.SetApiKeyEnabled"/> /
    /// <see cref="SystemBusinessObject.SetApiKeyExpiry"/>）的整合測試：列出不帶憑證素材、
    /// 停用即刻生效、遠端須為部署層管理員。
    /// </summary>
    /// <remarks>
    /// 每個測試用唯一 <c>sys_id</c> 並在 finally 清理——實體資料庫由多個平行測試行程共用。
    /// </remarks>
    public class SystemBusinessObjectApiKeyLifecycleTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectApiKeyLifecycleTests(SharedDbFixture fx) { _fx = fx; }

        private IDbConnectionManager ConnectionManager => _fx.GetRequiredService<IDbConnectionManager>();

        private SystemBusinessObject CreateBo()
            => new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty, SysProgIds.System);

        private static string NewSysId() => "life-" + Guid.NewGuid().ToString("N");

        private string IssueKey(string sysId, DateTime? expiredAt = null)
        {
            CreateBo().CreateApiKey(new CreateApiKeyArgs
            {
                SysId = sysId,
                SysName = "Lifecycle app",
                Contact = "ops@example.com",
                ExpiredAt = expiredAt,
            });
            return sysId;
        }

        private ApiKeySummary? Find(string sysId)
            => CreateBo().ListApiKeys(new ListApiKeysArgs())
                .ApiKeys.FirstOrDefault(k => k.SysId == sysId);

        private void DeleteKey(string sysId)
        {
            var dbType = ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string sql = $"DELETE FROM {dbType.QuoteIdentifier("st_api_key")} " +
                         $"WHERE {dbType.QuoteIdentifier("sys_id")} = {{0}}";
            new DbAccess(DbCategoryIds.Common, ConnectionManager)
                .Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, sysId));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("ListApiKeys 應列出已發放的金鑰，且不帶任何憑證素材")]
        public void ListApiKeys_ReturnsSummaryWithoutCredentialMaterial()
        {
            string sysId = IssueKey(NewSysId());
            try
            {
                var summary = Find(sysId);

                Assert.NotNull(summary);
                Assert.Equal("Lifecycle app", summary!.SysName);
                Assert.Equal("ops@example.com", summary.Contact);
                Assert.True(summary.Enabled);
                Assert.NotNull(summary.IssuedAt);
                // 型別上就沒有雜湊欄位——這裡釘住的是「別為了省事把 ApiKeyInfo 直接上 wire」。
                Assert.DoesNotContain("Hashed", typeof(ApiKeySummary).GetProperties().Select(p => p.Name));
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("ListApiKeys 應包含已停用的金鑰")]
        public void ListApiKeys_IncludesDisabledKeys()
        {
            string sysId = IssueKey(NewSysId());
            try
            {
                CreateBo().SetApiKeyEnabled(new SetApiKeyEnabledArgs { SysId = sysId, Enabled = false });

                // 停用的金鑰若從清單消失，該識別碼看起來就像沒被用過——而重發同一個識別碼
                // 正是不該悄悄發生的事。
                var summary = Find(sysId);
                Assert.NotNull(summary);
                Assert.False(summary!.Enabled);
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SetApiKeyEnabled 停用後金鑰應立即失效，不等快取過期")]
        public void SetApiKeyEnabled_Disable_RevokesImmediately()
        {
            string sysId = IssueKey(NewSysId());
            try
            {
                var repository = _fx.GetRequiredService<ISystemRepositoryFactory>().CreateApiKeyRepository();
                Assert.NotNull(repository.GetEnabledById(sysId));

                CreateBo().SetApiKeyEnabled(new SetApiKeyEnabledArgs { SysId = sysId, Enabled = false });

                // 撤銷若要等 ApiKeyCache 的 60 分鐘絕對過期才生效，那就不叫撤銷。
                Assert.Null(repository.GetEnabledById(sysId));
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SetApiKeyEnabled 重新啟用後金鑰應再度可用")]
        public void SetApiKeyEnabled_Reenable_RestoresKey()
        {
            string sysId = IssueKey(NewSysId());
            try
            {
                var bo = CreateBo();
                bo.SetApiKeyEnabled(new SetApiKeyEnabledArgs { SysId = sysId, Enabled = false });
                bo.SetApiKeyEnabled(new SetApiKeyEnabledArgs { SysId = sysId, Enabled = true });

                Assert.NotNull(_fx.GetRequiredService<ISystemRepositoryFactory>()
                    .CreateApiKeyRepository().GetEnabledById(sysId));
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SetApiKeyExpiry 應寫入到期時間，並可再清除")]
        public void SetApiKeyExpiry_SetsThenClears()
        {
            string sysId = IssueKey(NewSysId());
            try
            {
                var bo = CreateBo();
                var expiry = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                bo.SetApiKeyExpiry(new SetApiKeyExpiryArgs { SysId = sysId, ExpiredAt = expiry });
                Assert.Equal(expiry, Find(sysId)!.ExpiredAt);

                bo.SetApiKeyExpiry(new SetApiKeyExpiryArgs { SysId = sysId, ExpiredAt = null });
                Assert.Null(Find(sysId)!.ExpiredAt);
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SetApiKeyExpiry 接受已過去的時間——那是退役既有金鑰的正當手段")]
        public void SetApiKeyExpiry_PastExpiry_Accepted()
        {
            string sysId = IssueKey(NewSysId());
            try
            {
                var past = DateTime.UtcNow.AddMinutes(-1);

                // CreateApiKey 拒絕過去的到期（發一把出生即死的金鑰是失誤），
                // 但把既有金鑰設為此刻起失效是正當操作，兩者不該共用同一條規則。
                var result = CreateBo().SetApiKeyExpiry(new SetApiKeyExpiryArgs { SysId = sysId, ExpiredAt = past });

                Assert.Equal(sysId, result.SysId);
                Assert.NotNull(Find(sysId)!.ExpiredAt);
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SetApiKeyEnabled 於查無金鑰時應以可讀訊息拒絕")]
        public void SetApiKeyEnabled_UnknownKey_ThrowsUserMessage()
        {
            var args = new SetApiKeyEnabledArgs { SysId = "no-such-key", Enabled = false };

            var ex = Assert.Throws<UserMessageException>(() => CreateBo().SetApiKeyEnabled(args));
            Assert.Contains("no-such-key", ex.Message, StringComparison.Ordinal);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SetApiKeyExpiry 於查無金鑰時應以可讀訊息拒絕")]
        public void SetApiKeyExpiry_UnknownKey_ThrowsUserMessage()
        {
            var args = new SetApiKeyExpiryArgs { SysId = "no-such-key", ExpiredAt = null };

            Assert.Throws<UserMessageException>(() => CreateBo().SetApiKeyExpiry(args));
        }

        [Theory]
        [DisplayName("三個管理動作於遠端且非部署層管理員時皆應拒絕")]
        [InlineData("list")]
        [InlineData("enable")]
        [InlineData("expiry")]
        public void ManagementActions_RemoteNonAdmin_ThrowUnauthorized(string action)
        {
            var ctx = TestBeeContext.CreateWithOverrides(_fx,
                (typeof(IDeploymentAuthorizationService), new DenyingDeploymentAuthorization()));
            var bo = new SystemBusinessObject(ctx, Guid.NewGuid(), SysProgIds.System, isLocalCall: false);

            Assert.Throws<UnauthorizedAccessException>(() => Invoke(bo, action));
        }

        private static void Invoke(SystemBusinessObject bo, string action)
        {
            switch (action)
            {
                case "list":
                    bo.ListApiKeys(new ListApiKeysArgs());
                    break;
                case "enable":
                    bo.SetApiKeyEnabled(new SetApiKeyEnabledArgs { SysId = "any", Enabled = false });
                    break;
                default:
                    bo.SetApiKeyExpiry(new SetApiKeyExpiryArgs { SysId = "any", ExpiredAt = null });
                    break;
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("停用與設到期都應留下部署層稽核，且帶得出前後值")]
        public void LifecycleActions_WriteDeploymentAudit()
        {
            string sysId = IssueKey(NewSysId());
            try
            {
                var writer = new CapturingAuditLogWriter();
                var ctx = TestBeeContext.CreateWithOverrides(_fx,
                    (typeof(AuditLogOptions), new AuditLogOptions { Enabled = true }),
                    (typeof(IAuditLogWriter), writer));
                var bo = new SystemBusinessObject(ctx, Guid.Empty, SysProgIds.System);

                bo.SetApiKeyEnabled(new SetApiKeyEnabledArgs { SysId = sysId, Enabled = false });

                var entry = Assert.IsType<ChangeAuditEntry>(Assert.Single(writer.Entries));
                Assert.Equal("st_api_key", entry.ChangeTableName);
                Assert.Equal(sysId, entry.RowKey);
                Assert.Equal("System.SetApiKeyEnabled", entry.Source);
                Assert.True(entry.IsSensitive);

                var field = Assert.Single(ChangeDiffGramReader.Read(entry.ChangesXml));
                Assert.Equal("enabled", field.FieldName);
                Assert.Equal("True", field.OldValue);
                Assert.Equal("False", field.NewValue);
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        private sealed class DenyingDeploymentAuthorization : IDeploymentAuthorizationService
        {
            public bool Can(Guid accessToken, DeploymentAction action) => false;
        }

        private sealed class CapturingAuditLogWriter : IAuditLogWriter
        {
            public List<AuditEntry> Entries { get; } = [];

            public void Write(AuditEntry entry) => Entries.Add(entry);
        }
    }
}
