using System.ComponentModel;
using Bee.Base.Exceptions;
using Bee.Base.Security;
using Bee.Business.System;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Security;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject.CreateApiKey"/> 的整合測試：發放的金鑰能被驗證、
    /// 明文只出現一次（伺服端只存雜湊），以及輸入驗證的拒絕情境。
    /// </summary>
    /// <remarks>
    /// 每個測試用唯一 <c>sys_id</c> 並在 finally 清理——實體資料庫由多個平行測試行程共用。
    /// </remarks>
    public class SystemBusinessObjectApiKeyTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectApiKeyTests(SharedDbFixture fx) { _fx = fx; }

        private SystemBusinessObject CreateBo()
            => new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);

        private static string NewSysId() => "bo-" + Guid.NewGuid().ToString("N");

        private void DeleteKey(string sysId)
        {
            var connectionManager = _fx.GetRequiredService<IDbConnectionManager>();
            var dbType = connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string sql = $"DELETE FROM {dbType.QuoteIdentifier("st_api_key")} " +
                         $"WHERE {dbType.QuoteIdentifier("sys_id")} = {{0}}";
            new DbAccess(DbCategoryIds.Common, connectionManager)
                .Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, sysId));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("CreateApiKey 應回傳兩段式明文金鑰，且其 secret 對得上儲存的雜湊")]
        public void CreateApiKey_ReturnsPlaintextKeyMatchingStoredHash()
        {
            string sysId = NewSysId();
            try
            {
                var result = CreateBo().CreateApiKey(new CreateApiKeyArgs
                {
                    SysId = sysId,
                    SysName = "Issued app",
                    KeyType = ApiKeyType.ThirdParty,
                    Contact = "ops@example.com",
                });

                Assert.Equal(sysId, result.SysId);
                Assert.True(ApiKeyFormat.TryParse(result.ApiKey, out string parsedId, out string secret));
                Assert.Equal(sysId, parsedId);

                var stored = _fx.GetRequiredService<ISystemRepositoryFactory>()
                    .CreateApiKeyRepository().GetEnabledById(sysId);
                Assert.NotNull(stored);
                Assert.Equal("Issued app", stored!.SysName);
                Assert.Equal(ApiKeyType.ThirdParty, stored.KeyType);
                // Only the hash is persisted: the plaintext must never be recoverable from storage.
                Assert.DoesNotContain(secret, stored.HashedKey, StringComparison.Ordinal);
                Assert.True(ApiKeyHasher.VerifySecret(secret, stored.HashedKey));
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("CreateApiKey 對同一 sys_id 第二次應以可讀訊息拒絕，而非 unique index 錯誤")]
        public void CreateApiKey_DuplicateSysId_ThrowsUserMessage()
        {
            string sysId = NewSysId();
            var bo = CreateBo();
            try
            {
                bo.CreateApiKey(new CreateApiKeyArgs { SysId = sysId, SysName = "First" });

                var ex = Assert.Throws<UserMessageException>(() =>
                    bo.CreateApiKey(new CreateApiKeyArgs { SysId = sysId, SysName = "Second" }));

                Assert.Contains(sysId, ex.Message, StringComparison.Ordinal);
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [Theory]
        [DisplayName("CreateApiKey 於 sys_id 不合法時應拒絕（不得含分隔字元、不得大寫）")]
        [InlineData("")]
        [InlineData("ab")]
        [InlineData("Has-Upper")]
        [InlineData("has.dot")]
        [InlineData("-leading")]
        public void CreateApiKey_InvalidSysId_ThrowsUserMessage(string sysId)
        {
            var args = new CreateApiKeyArgs { SysId = sysId, SysName = "App" };

            Assert.Throws<UserMessageException>(() => CreateBo().CreateApiKey(args));
        }

        [Fact]
        [DisplayName("CreateApiKey 於未給應用程式名稱時應拒絕")]
        public void CreateApiKey_MissingSysName_ThrowsUserMessage()
        {
            var args = new CreateApiKeyArgs { SysId = NewSysId(), SysName = string.Empty };

            Assert.Throws<UserMessageException>(() => CreateBo().CreateApiKey(args));
        }

        [Fact]
        [DisplayName("CreateApiKey 於到期時間已過時應拒絕")]
        public void CreateApiKey_PastExpiry_ThrowsUserMessage()
        {
            var args = new CreateApiKeyArgs
            {
                SysId = NewSysId(),
                SysName = "App",
                ExpiredAt = DateTime.UtcNow.AddMinutes(-1),
            };

            Assert.Throws<UserMessageException>(() => CreateBo().CreateApiKey(args));
        }

        [Fact]
        [DisplayName("CreateApiKey 於 args 為 null 時應拋 ArgumentNullException")]
        public void CreateApiKey_NullArgs_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateBo().CreateApiKey(null!));
        }
    }
}
