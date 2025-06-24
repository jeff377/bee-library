namespace Bee.Base.UnitTests
{
    /// <summary>
    /// 測試 TPasswordHasher 密碼雜湊與驗證功能。
    /// </summary>
    public class PasswordHasherTests
    {
        /// <summary>
        /// 驗證相同密碼能正確通過雜湊驗證。
        /// </summary>
        [Fact]
        public void HashPassword_And_VerifyPassword_Should_Pass_With_Correct_Password()
        {
            // Arrange
            var hasher = new PasswordHasher();
            string originalPassword = "MySecurePassword!123";

            // Act
            string hashedPassword = hasher.HashPassword(originalPassword);
            bool result = hasher.VerifyPassword(originalPassword, hashedPassword);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 驗證錯誤密碼無法通過雜湊驗證。
        /// </summary>
        [Fact]
        public void VerifyPassword_Should_Fail_With_Incorrect_Password()
        {
            // Arrange
            var hasher = new PasswordHasher();
            string originalPassword = "MySecurePassword!123";
            string wrongPassword = "WrongPassword";
            string hashedPassword = hasher.HashPassword(originalPassword);

            // Act
            bool result = hasher.VerifyPassword(wrongPassword, hashedPassword);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 驗證格式錯誤的雜湊字串會返回驗證失敗。
        /// </summary>
        [Fact]
        public void VerifyPassword_Should_Fail_With_Invalid_Format()
        {
            // Arrange
            var hasher = new PasswordHasher();
            string password = "any";
            string invalidHash = "not.a.valid.hash";

            // Act
            bool result = hasher.VerifyPassword(password, invalidHash);

            // Assert
            Assert.False(result);
        }
    }
}
