using System.ComponentModel;
using Bee.Base.Security;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// 測試 PasswordHasher 密碼雜湊與驗證功能。
    /// </summary>
    public class PasswordHasherTests
    {
        /// <summary>
        /// 驗證相同密碼能正確通過雜湊驗證。
        /// </summary>
        [Fact]
        [DisplayName("正確密碼應通過雜湊驗證")]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            // Arrange
            string originalPassword = "MySecurePassword!123";

            // Act
            string hashedPassword = PasswordHasher.HashPassword(originalPassword);
            bool result = PasswordHasher.VerifyPassword(originalPassword, hashedPassword);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 驗證錯誤密碼無法通過雜湊驗證。
        /// </summary>
        [Fact]
        [DisplayName("錯誤密碼應無法通過雜湊驗證")]
        public void VerifyPassword_IncorrectPassword_ReturnsFalse()
        {
            // Arrange
            string originalPassword = "MySecurePassword!123";
            string wrongPassword = "WrongPassword";
            string hashedPassword = PasswordHasher.HashPassword(originalPassword);

            // Act
            bool result = PasswordHasher.VerifyPassword(wrongPassword, hashedPassword);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 驗證格式錯誤的雜湊字串會返回驗證失敗。
        /// </summary>
        [Fact]
        [DisplayName("無效格式的雜湊字串應回傳驗證失敗")]
        public void VerifyPassword_InvalidHashFormat_ReturnsFalse()
        {
            // Arrange
            string password = "any";
            string invalidHash = "not.a.valid.hash";

            // Act
            bool result = PasswordHasher.VerifyPassword(password, invalidHash);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 驗證 legacy SHA1 格式的雜湊字串可正確驗證（向下相容）。
        /// </summary>
        [Fact]
        [DisplayName("Legacy SHA1 格式雜湊應可通過驗證")]
        public void VerifyPassword_LegacySha1Format_ReturnsTrue()
        {
            // Arrange: 建立 legacy 格式 {iterations}.{saltBase64}.{hashBase64}（無 v2. 前綴）
            const string password = "LegacyPassword!";
            const int iterations = 10000;
            byte[] salt = new byte[16];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                rng.GetBytes(salt);
            byte[] hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                System.Text.Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                System.Security.Cryptography.HashAlgorithmName.SHA1,
                32);
            string legacyHash = $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";

            // Act
            bool correct = PasswordHasher.VerifyPassword(password, legacyHash);
            bool wrong = PasswordHasher.VerifyPassword("WrongPassword", legacyHash);

            // Assert
            Assert.True(correct);
            Assert.False(wrong);
        }

        /// <summary>
        /// v2 前綴但內部格式錯誤應回傳驗證失敗。
        /// </summary>
        [Fact]
        [DisplayName("v2 前綴但內部格式錯誤應回傳驗證失敗")]
        public void VerifyPassword_V2PrefixWithInvalidInner_ReturnsFalse()
        {
            // parts.Length != 3 → 直接 return false
            Assert.False(PasswordHasher.VerifyPassword("any", "v2.only.two"));
            // Base64 解析失敗 → catch 區段回傳 false
            Assert.False(PasswordHasher.VerifyPassword("any", "v2.100000.!!!bad-base64!!!.xx"));
        }
    }
}
