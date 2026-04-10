using System;
using System.ComponentModel;
using Xunit;
using Bee.Base;
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
            RsaCryptor.GenerateRsaKeyPair(out var publicKeyXml, out var privateKeyXml);
            string originalText = "aes-session-key-1234567890";

            // Act
            string encrypted = RsaCryptor.EncryptWithPublicKey(originalText, publicKeyXml);
            string decrypted = RsaCryptor.DecryptWithPrivateKey(encrypted, privateKeyXml);

            // Assert
            Assert.False(string.IsNullOrEmpty(encrypted));
            Assert.Equal(originalText, decrypted);
        }

        [Fact]
        [DisplayName("使用錯誤私鑰解密應擲出例外")]
        public void Decrypt_WrongPrivateKey_ThrowsException()
        {
            // Arrange
            RsaCryptor.GenerateRsaKeyPair(out var publicKeyXml1, out var privateKeyXml1);
            RsaCryptor.GenerateRsaKeyPair(out var publicKeyXml2, out var privateKeyXml2);

            string originalText = "this-will-fail";
            string encrypted = RsaCryptor.EncryptWithPublicKey(originalText, publicKeyXml1);

            // Act & Assert
            Assert.ThrowsAny<Exception>(() =>
            {
                var _ = RsaCryptor.DecryptWithPrivateKey(encrypted, privateKeyXml2);
            });
        }
    }
}
