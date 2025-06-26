namespace Bee.Base.UnitTests
{
    /// <summary>
    /// 測試 AesCbcHmacKeyGenerator 的金鑰產生與還原邏輯。
    /// </summary>
    public class AesCbcHmacKeyGeneratorTests
    {
        /// <summary>
        /// 驗證金鑰產生、Base64 編碼與還原後的 AES 與 HMAC 金鑰一致。
        /// </summary>
        [Fact]
        public void GenerateAndParseKey_ShouldBeValidAndConsistent()
        {
            // Arrange: 建立一組隨機 AES + HMAC 金鑰，並合併為 CombinedKey
            byte[] combinedKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();

            // Act: 拆解成 AES 與 HMAC 金鑰，再以 Base64 編碼並還原
            AesCbcHmacKeyGenerator.FromCombinedKey(combinedKey, out var aesKey1, out var hmacKey1);
            string base64 = Convert.ToBase64String(combinedKey);
            AesCbcHmacKeyGenerator.FromBase64CombinedKey(base64, out var aesKey2, out var hmacKey2);

            // Assert: 驗證 AES 與 HMAC 金鑰皆為 32 bytes，且還原後一致
            Assert.Equal(32, aesKey1.Length);
            Assert.Equal(32, hmacKey1.Length);
            Assert.Equal(aesKey1, aesKey2);
            Assert.Equal(hmacKey1, hmacKey2);
        }

        /// <summary>
        /// 驗證兩次產生的組合金鑰應不同，以確保亂數安全性。
        /// </summary>
        [Fact]
        public void GenerateCombinedKey_ShouldBeRandomEachTime()
        {
            // Arrange & Act: 產生兩組不同的組合金鑰
            var key1 = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            var key2 = AesCbcHmacKeyGenerator.GenerateCombinedKey();

            // Assert: Base64 表示的結果應不同
            Assert.NotEqual(Convert.ToBase64String(key1), Convert.ToBase64String(key2));
        }

        /// <summary>
        /// 驗證錯誤長度的組合金鑰應丟出 ArgumentException。
        /// </summary>
        [Fact]
        public void FromCombinedKey_WithInvalidLength_ShouldThrowException()
        {
            // Arrange: 構造不合法長度的金鑰資料
            var invalid = new byte[48];

            // Act & Assert: 呼叫還原方法應拋出例外
            Assert.Throws<ArgumentException>(() =>
                AesCbcHmacKeyGenerator.FromCombinedKey(invalid, out _, out _));
        }
    }
}
