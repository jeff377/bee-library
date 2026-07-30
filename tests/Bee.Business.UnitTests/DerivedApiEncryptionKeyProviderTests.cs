using System.ComponentModel;
using Bee.Business.Providers;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="DerivedApiEncryptionKeyProvider"/> 行為測試。
    /// 重點在「同一 token + 同一根金鑰必得同一把」——session 由 st_session 重建後
    /// 能取回可用金鑰，正是靠這個決定性。
    /// </summary>
    public class DerivedApiEncryptionKeyProviderTests
    {
        private static byte[] CreateRootKey(byte seed)
        {
            var key = new byte[64];
            for (int i = 0; i < key.Length; i++) key[i] = (byte)(i + seed);
            return key;
        }

        [Fact]
        [DisplayName("GetKey 應回傳 64 bytes")]
        public void GetKey_Returns64Bytes()
        {
            var provider = new DerivedApiEncryptionKeyProvider(CreateRootKey(0));

            var key = provider.GetKey(Guid.NewGuid());

            Assert.Equal(64, key.Length);
        }

        [Fact]
        [DisplayName("同一 token 與同一根金鑰導出結果應一致")]
        public void GetKey_SameTokenAndRootKey_ReturnsSameKey()
        {
            var token = Guid.NewGuid();
            var a = new DerivedApiEncryptionKeyProvider(CreateRootKey(0)).GetKey(token);
            var b = new DerivedApiEncryptionKeyProvider(CreateRootKey(0)).GetKey(token);

            Assert.Equal(a, b);
        }

        [Fact]
        [DisplayName("GenerateKeyForLogin 應與 GetKey 對同一 token 導出相同金鑰")]
        public void GenerateKeyForLogin_MatchesGetKey()
        {
            var provider = new DerivedApiEncryptionKeyProvider(CreateRootKey(0));
            var token = Guid.NewGuid();

            var generated = provider.GenerateKeyForLogin(token);
            var fetched = provider.GetKey(token);

            Assert.Equal(generated, fetched);
        }

        [Fact]
        [DisplayName("不同 token 應導出不同金鑰")]
        public void GetKey_DifferentTokens_ReturnDifferentKeys()
        {
            var provider = new DerivedApiEncryptionKeyProvider(CreateRootKey(0));

            var a = provider.GetKey(Guid.NewGuid());
            var b = provider.GetKey(Guid.NewGuid());

            Assert.NotEqual(a, b);
        }

        [Fact]
        [DisplayName("根金鑰不同應導出不同金鑰（根金鑰輪替使既有 session 失效）")]
        public void GetKey_DifferentRootKeys_ReturnDifferentKeys()
        {
            var token = Guid.NewGuid();

            var a = new DerivedApiEncryptionKeyProvider(CreateRootKey(0)).GetKey(token);
            var b = new DerivedApiEncryptionKeyProvider(CreateRootKey(7)).GetKey(token);

            Assert.NotEqual(a, b);
        }

        [Fact]
        [DisplayName("GetKey 傳入 Guid.Empty 應拋 UnauthorizedAccessException")]
        public void GetKey_EmptyToken_Throws()
        {
            var provider = new DerivedApiEncryptionKeyProvider(CreateRootKey(0));

            Assert.Throws<UnauthorizedAccessException>(() => provider.GetKey(Guid.Empty));
        }

        [Fact]
        [DisplayName("GenerateKeyForLogin 傳入 Guid.Empty 應拋 ArgumentException")]
        public void GenerateKeyForLogin_EmptyToken_Throws()
        {
            var provider = new DerivedApiEncryptionKeyProvider(CreateRootKey(0));

            Assert.Throws<ArgumentException>(() => provider.GenerateKeyForLogin(Guid.Empty));
        }

        [Fact]
        [DisplayName("FromMasterKey 應導出穩定且不等於 master key 本身的根金鑰")]
        public void FromMasterKey_DerivesStableRootKey()
        {
            var masterKey = CreateRootKey(3);
            var token = Guid.NewGuid();

            var a = DerivedApiEncryptionKeyProvider.FromMasterKey(masterKey).GetKey(token);
            var b = DerivedApiEncryptionKeyProvider.FromMasterKey(masterKey).GetKey(token);

            Assert.Equal(a, b);
            // 未設定 ApiEncryptionKey 時的退路，不得等同「直接拿 master key 當根金鑰」
            Assert.NotEqual(new DerivedApiEncryptionKeyProvider(masterKey).GetKey(token), a);
        }

        [Fact]
        [DisplayName("FromMasterKey 傳入空 master key 應拋 ArgumentException")]
        public void FromMasterKey_EmptyMasterKey_Throws()
        {
            Assert.Throws<ArgumentException>(() => DerivedApiEncryptionKeyProvider.FromMasterKey([]));
        }

        [Fact]
        [DisplayName("ctor 傳入 null 根金鑰應拋 ArgumentNullException")]
        public void Ctor_NullRootKey_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DerivedApiEncryptionKeyProvider(null!));
        }

        [Fact]
        [DisplayName("ctor 傳入空根金鑰應拋 ArgumentException")]
        public void Ctor_EmptyRootKey_Throws()
        {
            Assert.Throws<ArgumentException>(() => new DerivedApiEncryptionKeyProvider([]));
        }
    }
}
