using System.ComponentModel;
using Bee.Business.Providers;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="StaticApiEncryptionKeyProvider"/> 行為測試。
    /// Phase 4 之後 provider 透過 ctor 注入 byte[] 金鑰；測試也跟著用直接構造，不再操弄 BackendInfo 靜態。
    /// </summary>
    public class StaticApiEncryptionKeyProviderTests
    {
        [Fact]
        [DisplayName("GetKey 應回傳建構時注入的金鑰")]
        public void GetKey_ReturnsInjectedKey()
        {
            var key = new byte[64];
            for (int i = 0; i < key.Length; i++) key[i] = (byte)i;
            var provider = new StaticApiEncryptionKeyProvider(key);

            var actual = provider.GetKey(Guid.Empty);

            Assert.Same(key, actual);
        }

        [Fact]
        [DisplayName("GenerateKeyForLogin 應回傳與 GetKey 相同的共用金鑰")]
        public void GenerateKeyForLogin_ReturnsSameSharedKey()
        {
            var key = new byte[64];
            var provider = new StaticApiEncryptionKeyProvider(key);

            var a = provider.GetKey(Guid.NewGuid());
            var b = provider.GenerateKeyForLogin();

            Assert.Same(a, b);
        }

        [Fact]
        [DisplayName("ctor 傳入 null 金鑰應拋 ArgumentNullException")]
        public void Ctor_NullKey_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new StaticApiEncryptionKeyProvider(null!));
        }

        [Fact]
        [DisplayName("ctor 傳入空 byte[] 應拋 ArgumentException")]
        public void Ctor_EmptyKey_Throws()
        {
            Assert.Throws<ArgumentException>(() => new StaticApiEncryptionKeyProvider(Array.Empty<byte>()));
        }
    }
}
