using System.Text;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// 測試 CryptoFunc 類別的加密和雜湊功能。
    /// </summary>
    public class CryptoFuncTests
    {
        /// <summary>
        /// 測試 AES 256 加密和解密（指定 Key 和 IV）。
        /// </summary>
        [Fact]
        public void AesEncrypt_Decrypt_WithSpecifiedKeyAndIv_ReturnsOriginalData()
        {
            // Arrange
            string originalData = "Test string for AES encryption!";
            string key = "12345678901234567890123456789012"; // 32 bytes key
            string iv = "1234567890123456"; // 16 bytes IV

            // Act
            byte[] encryptedData = CryptoFunc.AesEncrypt(Encoding.UTF8.GetBytes(originalData), key, iv);
            byte[] decryptedData = CryptoFunc.AesDecrypt(encryptedData, key, iv);

            // Assert
            string decryptedString = Encoding.UTF8.GetString(decryptedData);
            Assert.Equal(originalData, decryptedString);  // 驗證解密後的資料與原始資料相同
        }

        /// <summary>
        /// 測試 AES 256 加密和解密（隨機生成 Key 和 IV）。
        /// </summary>
        [Fact]
        public void AesEncrypt_Decrypt_WithRandomKeyAndIv_ReturnsOriginalData()
        {
            // Arrange
            string originalData = "Test string for AES encryption!";

            // Act
            byte[] encryptedData = CryptoFunc.AesEncrypt(Encoding.UTF8.GetBytes(originalData));
            byte[] decryptedData = CryptoFunc.AesDecrypt(encryptedData);

            // Assert
            string decryptedString = Encoding.UTF8.GetString(decryptedData);
            Assert.Equal(originalData, decryptedString);  // 驗證解密後的資料與原始資料相同
        }

        /// <summary>
        /// 測試 SHA256 雜湊值是否一致，且格式正確。
        /// </summary>
        [Fact]
        public void Sha256Hash_ReturnsConsistentResult()
        {
            // Arrange
            string input = "password123";

            // Act
            string hash1 = CryptoFunc.Sha256Hash(input);
            string hash2 = CryptoFunc.Sha256Hash(input);
            string hashDifferent = CryptoFunc.Sha256Hash("password124");

            // Assert
            Assert.Equal(64, hash1.Length); // SHA256 為 256 bits = 64 hex chars
            Assert.Equal(hash1, hash2);     // 同樣輸入要一致
            Assert.NotEqual(hash1, hashDifferent); // 不同輸入不應相同
        }

        /// <summary>
        /// 測試 SHA512 雜湊值是否一致，且格式正確。
        /// </summary>
        [Fact]
        public void Sha512Hash_ReturnsConsistentResult()
        {
            // Arrange
            string input = "password123";

            // Act
            string hash1 = CryptoFunc.Sha512Hash(input);
            string hash2 = CryptoFunc.Sha512Hash(input);
            string hashDifferent = CryptoFunc.Sha512Hash("password124");

            // Assert
            Assert.Equal(128, hash1.Length); // SHA512 為 512 bits = 128 hex chars
            Assert.Equal(hash1, hash2);      // 同樣輸入要一致
            Assert.NotEqual(hash1, hashDifferent); // 不同輸入不應相同
        }


        // 測試 AES 256 解密失敗時的處理
        [Fact]
        public void AesTryDecrypt_InvalidData_ReturnsOriginalData()
        {
            // Arrange
            string encryptedData = "InvalidEncryptedData";

            // Act
            string result = CryptoFunc.AesTryDecrypt(encryptedData);

            // Assert
            Assert.Equal(encryptedData, result);  // 如果解密失敗，應該返回原始字串
        }
    }

}
