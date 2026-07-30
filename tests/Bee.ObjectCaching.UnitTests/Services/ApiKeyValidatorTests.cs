using System.ComponentModel;
using Bee.Base.Security;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.ObjectCaching.Services;

namespace Bee.ObjectCaching.UnitTests.Services
{
    /// <summary>
    /// <see cref="ApiKeyValidator"/> 的單元測試。每個測試使用獨立的
    /// <see cref="CacheContainerService"/>（唯一 prefix），可與其他 test class 平行執行。
    /// </summary>
    public class ApiKeyValidatorTests
    {
        private const string SysId = "northwind-desktop";
        private const string SysName = "Northwind Desktop";

        private sealed class StubCacheDataSourceProvider : ICacheDataSourceProvider
        {
            private readonly Func<string, ApiKeyInfo?>? _keyResolver;
            private readonly Func<ApiKeyGateState>? _gateResolver;

            public StubCacheDataSourceProvider(
                Func<string, ApiKeyInfo?>? keyResolver = null,
                Func<ApiKeyGateState>? gateResolver = null)
            {
                _keyResolver = keyResolver;
                _gateResolver = gateResolver;
            }

            public ApiKeyInfo? GetApiKey(string sysId) => _keyResolver?.Invoke(sysId);

            public ApiKeyGateState GetApiKeyGateState()
                => _gateResolver?.Invoke() ?? new ApiKeyGateState { InForce = true };

            public SessionInfo? GetSessionInfo(Guid accessToken) => null;
            public CompanyInfo? GetCompanyInfo(string companyId) => null;
            public CompanyRolePermissions? GetCompanyRolePermissions(string companyId) => null;
            public DepartmentTree? GetDepartmentTree(string companyId) => null;
        }

        private static CacheContainerService NewCache(ICacheDataSourceProvider? dataSource = null)
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var storage = new FileDefineStorage(paths);
            string prefix = "apikey_val_" + Guid.NewGuid().ToString("N");
            return dataSource == null
                ? new CacheContainerService(storage, paths, prefix)
                : new CacheContainerService(storage, paths, prefix, () => dataSource);
        }

        /// <summary>
        /// 建立一個 gate 在force、且指定 sys_id 對得上該 secret 的驗證器。
        /// </summary>
        private static ApiKeyValidator NewValidatorWithKey(string secret,
            DateTime? expiredAt = null, string sysId = SysId)
        {
            var info = new ApiKeyInfo
            {
                SysId = sysId,
                SysName = SysName,
                HashedKey = ApiKeyHasher.HashSecret(secret),
                ExpiredAt = expiredAt,
            };
            var dataSource = new StubCacheDataSourceProvider(
                keyResolver: id => id == sysId ? info : null);
            return new ApiKeyValidator(NewCache(dataSource));
        }

        [Fact]
        [DisplayName("建構子 cache 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullCache_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ApiKeyValidator(null!));
        }

        [Fact]
        [DisplayName("Validate 於金鑰正確時應回傳 Valid 並帶呼叫端識別")]
        public void Validate_MatchingKey_ReturnsValidWithCallerIdentity()
        {
            string secret = ApiKeyFormat.CreateSecret();
            var validator = NewValidatorWithKey(secret);

            var result = validator.Validate(ApiKeyFormat.Compose(SysId, secret));

            Assert.Equal(ApiKeyStatus.Valid, result.Status);
            Assert.True(result.IsAccepted);
            Assert.Equal(SysId, result.SysId);
            Assert.Equal(SysName, result.SysName);
        }

        [Fact]
        [DisplayName("Validate 於 secret 不符時應回傳 Invalid")]
        public void Validate_WrongSecret_ReturnsInvalid()
        {
            var validator = NewValidatorWithKey(ApiKeyFormat.CreateSecret());

            var result = validator.Validate(ApiKeyFormat.Compose(SysId, ApiKeyFormat.CreateSecret()));

            Assert.Equal(ApiKeyStatus.Invalid, result.Status);
            Assert.False(result.IsAccepted);
        }

        [Fact]
        [DisplayName("Validate 於 sys_id 查無(含停用，repository 一併排除)時應回傳 Invalid")]
        public void Validate_UnknownSysId_ReturnsInvalid()
        {
            string secret = ApiKeyFormat.CreateSecret();
            var validator = NewValidatorWithKey(secret);

            var result = validator.Validate(ApiKeyFormat.Compose("other-app", secret));

            Assert.Equal(ApiKeyStatus.Invalid, result.Status);
        }

        [Fact]
        [DisplayName("Validate 於金鑰已過期時應回傳 Invalid(即時判定，不靠快取過期)")]
        public void Validate_ExpiredKey_ReturnsInvalid()
        {
            string secret = ApiKeyFormat.CreateSecret();
            var validator = NewValidatorWithKey(secret, expiredAt: DateTime.UtcNow.AddMinutes(-1));

            var result = validator.Validate(ApiKeyFormat.Compose(SysId, secret));

            Assert.Equal(ApiKeyStatus.Invalid, result.Status);
        }

        [Fact]
        [DisplayName("Validate 於金鑰到期時間未到時應回傳 Valid")]
        public void Validate_NotYetExpiredKey_ReturnsValid()
        {
            string secret = ApiKeyFormat.CreateSecret();
            var validator = NewValidatorWithKey(secret, expiredAt: DateTime.UtcNow.AddHours(1));

            var result = validator.Validate(ApiKeyFormat.Compose(SysId, secret));

            Assert.Equal(ApiKeyStatus.Valid, result.Status);
        }

        [Theory]
        [DisplayName("Validate 於格式不符時應回傳 Invalid 且不查資料來源")]
        [InlineData("no-separator")]
        [InlineData("Bad-SysId.secret")]
        [InlineData(".secret")]
        public void Validate_MalformedKey_ReturnsInvalidWithoutHittingDataSource(string apiKey)
        {
            var dataSource = new StubCacheDataSourceProvider(
                keyResolver: _ => throw new InvalidOperationException("should not be called"));
            var validator = new ApiKeyValidator(NewCache(dataSource));

            var result = validator.Validate(apiKey);

            Assert.Equal(ApiKeyStatus.Invalid, result.Status);
            Assert.Equal(string.Empty, result.SysId);
        }

        [Theory]
        [DisplayName("Validate 於 gate 在force 但未帶金鑰時應回傳 NotProvided")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_GateInForce_NoKey_ReturnsNotProvided(string? apiKey)
        {
            var validator = new ApiKeyValidator(NewCache(new StubCacheDataSourceProvider()));

            var result = validator.Validate(apiKey);

            Assert.Equal(ApiKeyStatus.NotProvided, result.Status);
            Assert.False(result.IsAccepted);
        }

        [Fact]
        [DisplayName("Validate 於無啟用金鑰時應回傳 NotConfigured(相容態)")]
        public void Validate_GateNotInForce_ReturnsNotConfigured()
        {
            var dataSource = new StubCacheDataSourceProvider(
                gateResolver: () => new ApiKeyGateState { InForce = false });
            var validator = new ApiKeyValidator(NewCache(dataSource));

            var result = validator.Validate("anything");

            Assert.Equal(ApiKeyStatus.NotConfigured, result.Status);
            Assert.True(result.IsAccepted);
        }

        [Fact]
        [DisplayName("Validate 於快取無資料來源(無金鑰存放處)時應回傳 NotConfigured")]
        public void Validate_NoDataSource_ReturnsNotConfigured()
        {
            var validator = new ApiKeyValidator(NewCache());

            var result = validator.Validate("anything");

            Assert.Equal(ApiKeyStatus.NotConfigured, result.Status);
        }

        [Fact]
        [DisplayName("Validate 於資料來源擲例外時應向外傳播，由呼叫端 fail closed")]
        public void Validate_DataSourceThrows_PropagatesForFailClosed()
        {
            var dataSource = new StubCacheDataSourceProvider(
                gateResolver: () => throw new InvalidOperationException("store unreachable"));
            var validator = new ApiKeyValidator(NewCache(dataSource));

            Assert.Throws<InvalidOperationException>(() => validator.Validate("anything"));
        }

        [Fact]
        [DisplayName("Validate 應對已快取的金鑰重複命中，不重複查資料來源")]
        public void Validate_RepeatedCalls_LoadsKeyOnce()
        {
            string secret = ApiKeyFormat.CreateSecret();
            int callCount = 0;
            var info = new ApiKeyInfo
            {
                SysId = SysId,
                SysName = SysName,
                HashedKey = ApiKeyHasher.HashSecret(secret),
            };
            var dataSource = new StubCacheDataSourceProvider(keyResolver: _ =>
            {
                callCount++;
                return info;
            });
            var validator = new ApiKeyValidator(NewCache(dataSource));
            string apiKey = ApiKeyFormat.Compose(SysId, secret);

            Assert.Equal(ApiKeyStatus.Valid, validator.Validate(apiKey).Status);
            Assert.Equal(ApiKeyStatus.Valid, validator.Validate(apiKey).Status);

            Assert.Equal(1, callCount);
        }
    }
}
