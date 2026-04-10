using System.ComponentModel;
using System.Security.Cryptography;
using Bee.Api.Core.Transformer;
using Bee.Base.Security;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// AesPayloadEncryptor 安全性測試
    /// </summary>
    public class AesPayloadEncryptorTests
    {
        private readonly AesPayloadEncryptor encryptor = new AesPayloadEncryptor();

        [Fact(DisplayName = "Encrypt 使用 null Key 應拋出 CryptographicException")]
        public void Encrypt_NullKey_ThrowsCryptographicException()
        {
            var data = new byte[] { 1, 2, 3 };

            Assert.Throws<CryptographicException>(() =>
                encryptor.Encrypt(data, null));
        }

        [Fact(DisplayName = "Encrypt 使用空 Key 應拋出 CryptographicException")]
        public void Encrypt_EmptyKey_ThrowsCryptographicException()
        {
            var data = new byte[] { 1, 2, 3 };

            Assert.Throws<CryptographicException>(() =>
                encryptor.Encrypt(data, new byte[0]));
        }

        [Fact(DisplayName = "Decrypt 使用 null Key 應拋出 CryptographicException")]
        public void Decrypt_NullKey_ThrowsCryptographicException()
        {
            var data = new byte[] { 1, 2, 3 };

            Assert.Throws<CryptographicException>(() =>
                encryptor.Decrypt(data, null));
        }

        [Fact(DisplayName = "Decrypt 使用空 Key 應拋出 CryptographicException")]
        public void Decrypt_EmptyKey_ThrowsCryptographicException()
        {
            var data = new byte[] { 1, 2, 3 };

            Assert.Throws<CryptographicException>(() =>
                encryptor.Decrypt(data, new byte[0]));
        }

        [Fact(DisplayName = "Encrypt/Decrypt 使用有效 Key 應正確加解密")]
        public void Encrypt_Decrypt_ValidKey_RoundTrip()
        {
            var originalData = new byte[] { 10, 20, 30, 40, 50 };
            var key = AesCbcHmacKeyGenerator.GenerateCombinedKey();

            var encrypted = encryptor.Encrypt(originalData, key);
            var decrypted = encryptor.Decrypt(encrypted, key);

            Assert.Equal(originalData, decrypted);
        }
    }
}
