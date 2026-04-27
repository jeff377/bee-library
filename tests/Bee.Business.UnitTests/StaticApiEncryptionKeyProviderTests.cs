using System.ComponentModel;
using Bee.Business.Providers;
using Bee.Definition;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="StaticApiEncryptionKeyProvider"/> 行為測試。
    /// </summary>
    [Collection("Initialize")]
    public class StaticApiEncryptionKeyProviderTests
    {
        [Fact]
        [DisplayName("GetKey 應回傳 BackendInfo.ApiEncryptionKey")]
        public void GetKey_ReturnsBackendKey()
        {
            var provider = new StaticApiEncryptionKeyProvider();

            var key = provider.GetKey(Guid.Empty);

            Assert.NotNull(key);
            Assert.Same(BackendInfo.ApiEncryptionKey, key);
        }

        [Fact]
        [DisplayName("GenerateKeyForLogin 應回傳與 GetKey 相同的共用金鑰")]
        public void GenerateKeyForLogin_ReturnsSameSharedKey()
        {
            var provider = new StaticApiEncryptionKeyProvider();

            var a = provider.GetKey(Guid.NewGuid());
            var b = provider.GenerateKeyForLogin();

            Assert.Same(a, b);
        }

        [Fact]
        [DisplayName("BackendInfo.ApiEncryptionKey 未初始化時 GetKey 應拋 InvalidOperationException")]
        public void GetKey_BackendKeyNull_ThrowsInvalidOperation()
        {
            var provider = new StaticApiEncryptionKeyProvider();
            var backup = BackendInfo.ApiEncryptionKey;

            try
            {
                BackendInfo.ApiEncryptionKey = null!;
                Assert.Throws<InvalidOperationException>(() => provider.GetKey(Guid.Empty));
            }
            finally
            {
                BackendInfo.ApiEncryptionKey = backup;
            }
        }
    }
}
