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
    }
}
