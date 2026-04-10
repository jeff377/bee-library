using System.ComponentModel;
using Bee.Base.Security;
using System.Security.Cryptography;
using System.Text;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// AesCbcHmacCryptor 加解密邏輯測試
    /// </summary>
    public class AesCbcHmacCryptorTests
    {
        private readonly byte[] _aesKey = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"); // 32 bytes
        private readonly byte[] _hmacKey = Encoding.UTF8.GetBytes("abcdef0123456789abcdef0123456789"); // 32 bytes

        [Fact]
        [DisplayName("加密後解密應還原為原始明文")]
        public void EncryptAndDecrypt_ValidPlaintext_ReturnsOriginalText()
        {
            // Arrange
            string originalText = "Bee.NET 測試資料內容";
            byte[] plainBytes = Encoding.UTF8.GetBytes(originalText);

            // Act
            byte[] encrypted = AesCbcHmacCryptor.Encrypt(plainBytes, _aesKey, _hmacKey);
            byte[] decrypted = AesCbcHmacCryptor.Decrypt(encrypted, _aesKey, _hmacKey);
            string decryptedText = Encoding.UTF8.GetString(decrypted);

            // Assert
            Assert.Equal(originalText, decryptedText);
        }

        [Fact]
        [DisplayName("相同明文加密兩次應產生不同密文")]
        public void Encrypt_SamePlaintext_ProducesDifferentCiphertext()
        {
            // Arrange
            byte[] plainBytes = Encoding.UTF8.GetBytes("相同內容測試");

            // Act
            byte[] encrypted1 = AesCbcHmacCryptor.Encrypt(plainBytes, _aesKey, _hmacKey);
            byte[] encrypted2 = AesCbcHmacCryptor.Encrypt(plainBytes, _aesKey, _hmacKey);

            // Assert
            Assert.NotEqual(Convert.ToBase64String(encrypted1), Convert.ToBase64String(encrypted2));
        }

        [Fact]
        [DisplayName("解密被竄改的密文應擲出 CryptographicException")]
        public void Decrypt_TamperedCiphertext_ThrowsCryptographicException()
        {
            // Arrange
            byte[] plainBytes = Encoding.UTF8.GetBytes("敏感資料");
            byte[] encrypted = AesCbcHmacCryptor.Encrypt(plainBytes, _aesKey, _hmacKey);

            // 模擬篡改內容
            encrypted[encrypted.Length - 10] ^= 0xFF;

            // Act & Assert
            Assert.Throws<CryptographicException>(() =>
            {
                AesCbcHmacCryptor.Decrypt(encrypted, _aesKey, _hmacKey);
            });
        }
    }
}
