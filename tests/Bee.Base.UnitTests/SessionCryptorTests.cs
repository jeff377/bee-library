namespace Bee.Base.UnitTests
{
    /// <summary>
    /// 測試 SessionCryptor 類別的 AES 金鑰產生與解析功能。
    /// </summary>
    public class SessionCryptorTests
    {
        /// <summary>
        /// 驗證產生的 SessionKey 是否為有效的 Base64 字串，且包含 32-byte Key + 16-byte IV。
        /// </summary>
        [Fact]
        public void GenerateSessionKey_ShouldProduceValidBase64WithKeyAndIV()
        {
            // Arrange & Act
            string sessionKey = SessionCryptor.GenerateSessionKey();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(sessionKey));

            byte[] allBytes = Convert.FromBase64String(sessionKey);

            Assert.Equal(48, allBytes.Length); // 32-byte key + 16-byte IV
        }

        /// <summary>
        /// 驗證解析 SessionKey 時，是否能正確取得 Key 與 IV，並符合長度。
        /// </summary>
        [Fact]
        public void ParseSessionKey_ShouldReturnCorrectKeyAndIV()
        {
            // Arrange
            string sessionKey = SessionCryptor.GenerateSessionKey();

            // Act
            SessionCryptor.ParseSessionKey(sessionKey, out byte[] key, out byte[] iv);

            // Assert
            Assert.NotNull(key);
            Assert.NotNull(iv);
            Assert.Equal(32, key.Length);
            Assert.Equal(16, iv.Length);
        }

        /// <summary>
        /// 驗證 SessionKey 經過解析與重新組合後，其內容與原始一致。
        /// </summary>
        [Fact]
        public void GenerateAndParse_ShouldBeConsistent()
        {
            // Arrange
            string sessionKey = SessionCryptor.GenerateSessionKey();
            byte[] originalBytes = Convert.FromBase64String(sessionKey);

            // Act
            SessionCryptor.ParseSessionKey(sessionKey, out byte[] key, out byte[] iv);
            byte[] recombined = new byte[48];
            Buffer.BlockCopy(key, 0, recombined, 0, key.Length);
            Buffer.BlockCopy(iv, 0, recombined, key.Length, iv.Length);

            // Assert
            Assert.Equal(originalBytes, recombined);
        }
    }
}
