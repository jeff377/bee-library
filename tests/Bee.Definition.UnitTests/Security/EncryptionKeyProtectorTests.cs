using System.ComponentModel;
using Bee.Base.Security;
using Bee.Definition.Security;

namespace Bee.Definition.UnitTests.Security
{
    /// <summary>
    /// EncryptionKeyProtector 正常與錯誤路徑測試。
    /// </summary>
    public class EncryptionKeyProtectorTests
    {
        [Fact]
        [DisplayName("GenerateEncryptedKey 使用有效 Master Key 應可後續解密還原")]
        public void GenerateEncryptedKey_ValidMasterKey_CanBeDecrypted()
        {
            // Arrange
            byte[] masterKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();

            // Act
            string base64 = EncryptionKeyProtector.GenerateEncryptedKey(masterKey);
            byte[] decrypted = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, base64);

            // Assert
            Assert.NotEmpty(base64);
            Assert.Equal(64, decrypted.Length); // 256-bit AES + 256-bit HMAC = 64 bytes
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new byte[0])]
        [DisplayName("GenerateEncryptedKey 傳入空 Master Key 應拋出 ArgumentException")]
        public void GenerateEncryptedKey_EmptyMasterKey_ThrowsArgumentException(byte[]? masterKey)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => EncryptionKeyProtector.GenerateEncryptedKey(masterKey!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new byte[0])]
        [DisplayName("DecryptEncryptedKey 傳入空 Master Key 應拋出 ArgumentException")]
        public void DecryptEncryptedKey_EmptyMasterKey_ThrowsArgumentException(byte[]? masterKey)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                EncryptionKeyProtector.DecryptEncryptedKey(masterKey!, "SGVsbG8="));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("DecryptEncryptedKey 密文為空應拋出 ArgumentException")]
        public void DecryptEncryptedKey_EmptyCipherText_ThrowsArgumentException(string? cipherText)
        {
            // Arrange
            byte[] masterKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                EncryptionKeyProtector.DecryptEncryptedKey(masterKey, cipherText!));
        }

        [Fact]
        [DisplayName("DecryptEncryptedKey 使用錯誤的 Master Key 應拋出例外")]
        public void DecryptEncryptedKey_WrongMasterKey_Throws()
        {
            // Arrange
            byte[] originalKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            byte[] wrongKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            string base64 = EncryptionKeyProtector.GenerateEncryptedKey(originalKey);

            // Act & Assert
            Assert.ThrowsAny<Exception>(() =>
                EncryptionKeyProtector.DecryptEncryptedKey(wrongKey, base64));
        }

        [Fact]
        [DisplayName("DecryptEncryptedKey 密文非 Base64 格式應拋出 FormatException")]
        public void DecryptEncryptedKey_NonBase64CipherText_ThrowsFormatException()
        {
            // Arrange
            byte[] masterKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();

            // Act & Assert
            Assert.Throws<FormatException>(() =>
                EncryptionKeyProtector.DecryptEncryptedKey(masterKey, "@@not-base64@@"));
        }
    }
}
