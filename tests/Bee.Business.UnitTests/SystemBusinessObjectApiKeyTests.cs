using System.ComponentModel;
using Bee.Base.Exceptions;
using Bee.Base.Security;
using Bee.Business.System;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject.CreateApiKey"/> 的整合測試：發放的金鑰能被驗證、
    /// 明文只出現一次（伺服端只存雜湊）、輸入驗證的拒絕情境，以及遠端呼叫的部署層授權把關。
    /// </summary>
    /// <remarks>
    /// 每個測試用唯一 <c>sys_id</c> 並在 finally 清理——實體資料庫由多個平行測試行程共用。
    /// 需要使用者列的測試一律自建（見 <see cref="TestUsers"/>），不動 seed 使用者 '001'。
    /// </remarks>
    public class SystemBusinessObjectApiKeyTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectApiKeyTests(SharedDbFixture fx) { _fx = fx; }

        /// <summary>
        /// 本機呼叫端（<c>isLocalCall</c> 預設 true），即部署期在主機上的呼叫路徑。
        /// </summary>
        private SystemBusinessObject CreateBo()
            => new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);

        private static string NewSysId() => "bo-" + Guid.NewGuid().ToString("N");

        private IDbConnectionManager ConnectionManager => _fx.GetRequiredService<IDbConnectionManager>();

        private ISessionInfoService SessionInfoService => _fx.GetRequiredService<ISessionInfoService>();

        private void DeleteKey(string sysId)
        {
            var dbType = ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string sql = $"DELETE FROM {dbType.QuoteIdentifier("st_api_key")} " +
                         $"WHERE {dbType.QuoteIdentifier("sys_id")} = {{0}}";
            new DbAccess(DbCategoryIds.Common, ConnectionManager)
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

        #region 遠端呼叫的部署層授權把關

        [Fact]
        [DisplayName("CreateApiKey 遠端呼叫且非部署層管理員時應拒絕")]
        public void CreateApiKey_RemoteNonAdmin_ThrowsUnauthorized()
        {
            var ctx = TestBeeContext.CreateWithOverrides(_fx,
                (typeof(IDeploymentAuthorizationService), new FakeDeploymentAuthorization(allowed: false)));
            var bo = new SystemBusinessObject(ctx, Guid.NewGuid(), isLocalCall: false);

            // 授權在輸入驗證之前：合法的 args 也照樣被擋，拒絕的理由不會被輸入錯誤蓋掉。
            Assert.Throws<UnauthorizedAccessException>(() =>
                bo.CreateApiKey(new CreateApiKeyArgs { SysId = NewSysId(), SysName = "App" }));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("CreateApiKey 遠端呼叫且為部署層管理員時應發放金鑰")]
        public void CreateApiKey_RemoteDeploymentAdmin_IssuesKey()
        {
            string userId = TestUsers.Create(ConnectionManager, "apikey-adm");
            string sysId = NewSysId();
            Guid token = Guid.Empty;
            try
            {
                CreateBo().SetDeploymentAdmin(new SetDeploymentAdminArgs
                {
                    UserId = userId,
                    IsDeploymentAdmin = true,
                });
                token = NewSession(userId);

                var result = RemoteBo(token).CreateApiKey(new CreateApiKeyArgs
                {
                    SysId = sysId,
                    SysName = "Remotely issued",
                });

                Assert.Equal(sysId, result.SysId);
                Assert.True(ApiKeyFormat.TryParse(result.ApiKey, out _, out _));
            }
            finally
            {
                Cleanup(token, userId, sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("CreateApiKey 遠端呼叫時，僅『已登入』的一般使用者仍應被拒")]
        public void CreateApiKey_RemoteAuthenticatedUserWithoutFlag_ThrowsUnauthorized()
        {
            string userId = TestUsers.Create(ConnectionManager, "apikey-usr");
            string sysId = NewSysId();
            Guid token = Guid.Empty;
            try
            {
                // 有效 session、旗標為預設值 false——這正是升級後仍必須擋住的情境。
                token = NewSession(userId);

                Assert.Throws<UnauthorizedAccessException>(() =>
                    RemoteBo(token).CreateApiKey(new CreateApiKeyArgs { SysId = sysId, SysName = "App" }));
            }
            finally
            {
                Cleanup(token, userId, sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("CreateApiKey 本機呼叫免管理員，維持首把金鑰的 bootstrap 路徑")]
        public void CreateApiKey_LocalCallWithoutAdmin_IssuesKey()
        {
            string userId = TestUsers.Create(ConnectionManager, "apikey-loc");
            string sysId = NewSysId();
            Guid token = Guid.Empty;
            try
            {
                token = NewSession(userId);

                // 尚無管理員的部署必須鑄得出第一把金鑰，否則階段 1 保留的 bootstrap 路徑就斷了。
                var result = new SystemBusinessObject(TestBeeContext.Create(_fx), token)
                    .CreateApiKey(new CreateApiKeyArgs { SysId = sysId, SysName = "Bootstrap" });

                Assert.Equal(sysId, result.SysId);
            }
            finally
            {
                Cleanup(token, userId, sysId);
            }
        }

        /// <summary>
        /// 遠端呼叫端（<c>isLocalCall: false</c>），走真實的 <see cref="IDeploymentAuthorizationService"/>。
        /// </summary>
        private SystemBusinessObject RemoteBo(Guid accessToken)
            => new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken, isLocalCall: false);

        private Guid NewSession(string userId)
            => CreateBo().CreateSession(new CreateSessionArgs { UserID = userId, ExpiresIn = 600 }).AccessToken;

        private void Cleanup(Guid token, string userId, string sysId)
        {
            if (token != Guid.Empty)
                SessionInfoService.Remove(token);
            TestUsers.Delete(ConnectionManager, userId);
            DeleteKey(sysId);
        }

        private sealed class FakeDeploymentAuthorization : IDeploymentAuthorizationService
        {
            private readonly bool _allowed;

            public FakeDeploymentAuthorization(bool allowed) { _allowed = allowed; }

            public bool Can(Guid accessToken, DeploymentAction action) => _allowed;
        }

        #endregion
    }
}
