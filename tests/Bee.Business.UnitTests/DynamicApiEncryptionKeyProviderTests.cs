using System.ComponentModel;
using Bee.Business.Providers;
using Bee.Definition.Identity;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="DynamicApiEncryptionKeyProvider"/> 行為測試。
    /// </summary>
    public class DynamicApiEncryptionKeyProviderTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public DynamicApiEncryptionKeyProviderTests(SharedDbFixture fx) { _fx = fx; }
        private DynamicApiEncryptionKeyProvider CreateProvider()
            => new(_fx.GetRequiredService<ISessionInfoService>());

        [Fact]
        [DisplayName("GetKey(Guid.Empty) 應拋 UnauthorizedAccessException")]
        public void GetKey_Empty_ThrowsUnauthorized()
        {
            var provider = CreateProvider();
            Assert.Throws<UnauthorizedAccessException>(() => provider.GetKey(Guid.Empty));
        }

        [Fact]
        [DisplayName("GetKey 未知 AccessToken 應拋 UnauthorizedAccessException")]
        public void GetKey_UnknownToken_ThrowsUnauthorized()
        {
            var provider = CreateProvider();
            var unknownToken = Guid.NewGuid();

            Assert.Throws<UnauthorizedAccessException>(() => provider.GetKey(unknownToken));
        }

        [Fact]
        [DisplayName("GetKey 有效 Session 應回傳對應的 ApiEncryptionKey")]
        public void GetKey_ValidSession_ReturnsKey()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var provider = new DynamicApiEncryptionKeyProvider(sessionService);
            var token = Guid.NewGuid();
            var key = new byte[64];
            for (int i = 0; i < key.Length; i++) key[i] = (byte)i;

            var session = new SessionInfo
            {
                AccessToken = token,
                UserId = "u01",
                UserName = "U01",
                ExpiredAt = DateTime.UtcNow.AddHours(1),
                ApiEncryptionKey = key
            };
            sessionService.Set(session);

            try
            {
                var actual = provider.GetKey(token);
                Assert.Equal(key, actual);
            }
            finally
            {
                sessionService.Remove(token);
            }
        }

        [Fact]
        [DisplayName("GenerateKeyForLogin 應回傳 64 bytes")]
        public void GenerateKeyForLogin_Returns64Bytes()
        {
            var provider = CreateProvider();

            var key = provider.GenerateKeyForLogin();

            Assert.NotNull(key);
            Assert.Equal(64, key.Length);
        }
    }
}
