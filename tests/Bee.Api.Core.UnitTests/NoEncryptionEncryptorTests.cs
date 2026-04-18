using System.ComponentModel;
using Bee.Api.Core.Transformer;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// NoEncryptionEncryptor 測試。僅測試行為；實際安全規範禁止在 production 使用此類別。
    /// </summary>
    public class NoEncryptionEncryptorTests
    {
        [Fact]
        [DisplayName("EncryptionMethod 應為 \"none\"")]
        public void EncryptionMethod_IsNone()
        {
            var encryptor = new NoEncryptionEncryptor();

            Assert.Equal("none", encryptor.EncryptionMethod);
        }

        [Fact]
        [DisplayName("Encrypt 應回傳原始 byte 陣列")]
        public void Encrypt_ReturnsSameBytes()
        {
            var encryptor = new NoEncryptionEncryptor();
            var data = new byte[] { 1, 2, 3, 4, 5 };
            var key = new byte[] { 9, 8, 7 };

            var result = encryptor.Encrypt(data, key);

            Assert.Same(data, result);
        }

        [Fact]
        [DisplayName("Decrypt 應回傳原始 byte 陣列")]
        public void Decrypt_ReturnsSameBytes()
        {
            var encryptor = new NoEncryptionEncryptor();
            var data = new byte[] { 1, 2, 3, 4, 5 };
            var key = new byte[] { 9, 8, 7 };

            var result = encryptor.Decrypt(data, key);

            Assert.Same(data, result);
        }
    }
}
