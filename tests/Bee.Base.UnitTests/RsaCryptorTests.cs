using System;
using Xunit;
using Bee.Base;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// RSA 加解密測試
    /// </summary>
    public class RsaCryptorTests
    {
        [Fact]
        public void Rsa_Encrypt_Decrypt_Should_Succeed()
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
        public void Rsa_Decrypt_With_Wrong_Key_Should_Throw()
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
