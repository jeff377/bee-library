using System.ComponentModel;
using Bee.Base.Security;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// RSA 加解密測試
    /// </summary>
    public class RsaCryptorTests
    {
        [Fact]
        [DisplayName("RSA 公鑰加密後以私鑰解密應還原為原始明文")]
        public void EncryptAndDecrypt_ValidKeyPair_ReturnsOriginalText()
        {
            // Arrange
            RsaCryptor.GenerateRsaKeyPair(out var publicKey, out var privateKey);
            string originalText = "aes-session-key-1234567890";

            // Act
            string encrypted = RsaCryptor.EncryptWithPublicKey(originalText, publicKey);
            string decrypted = RsaCryptor.DecryptWithPrivateKey(encrypted, privateKey);

            // Assert
            Assert.False(string.IsNullOrEmpty(encrypted));
            Assert.Equal(originalText, decrypted);
        }

        [Fact]
        [DisplayName("使用錯誤私鑰解密應擲出例外")]
        public void Decrypt_WrongPrivateKey_ThrowsException()
        {
            // Arrange
            RsaCryptor.GenerateRsaKeyPair(out var publicKey1, out _);
            RsaCryptor.GenerateRsaKeyPair(out _, out var privateKey2);

            string originalText = "this-will-fail";
            string encrypted = RsaCryptor.EncryptWithPublicKey(originalText, publicKey1);

            // Act & Assert
            Assert.ThrowsAny<Exception>(() =>
            {
                var _ = RsaCryptor.DecryptWithPrivateKey(encrypted, privateKey2);
            });
        }

        [Fact]
        [DisplayName("GenerateRsaKeyPair 應產出 PEM 格式字串(SPKI public、PKCS#1 private)")]
        public void GenerateRsaKeyPair_ReturnsPemFormattedStrings()
        {
            RsaCryptor.GenerateRsaKeyPair(out var publicKey, out var privateKey);

            Assert.StartsWith("-----BEGIN PUBLIC KEY-----", publicKey);
            Assert.Contains("-----END PUBLIC KEY-----", publicKey);
            Assert.StartsWith("-----BEGIN RSA PRIVATE KEY-----", privateKey);
            Assert.Contains("-----END RSA PRIVATE KEY-----", privateKey);
        }
    }
}
