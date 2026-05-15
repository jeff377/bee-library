using System.ComponentModel;
using Bee.Definition.Identity;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// SessionInfo / SessionUser / UserInfo 等 DTO 基本屬性測試。
    /// </summary>
    public class DtoPropertyTests
    {
        [Fact]
        [DisplayName("SessionInfo GetKey 應回傳 AccessToken 的字串表示")]
        public void SessionInfo_GetKey_ReturnsAccessTokenString()
        {
            // Arrange
            var token = Guid.NewGuid();
            var info = new SessionInfo { AccessToken = token };

            // Act & Assert
            Assert.Equal(token.ToString(), info.GetKey());
        }

        [Fact]
        [DisplayName("SessionInfo 預設值應為空 Guid 與 zh-TW 文化")]
        public void SessionInfo_Defaults_ReturnsExpectedValues()
        {
            // Act
            var info = new SessionInfo();

            // Assert
            Assert.Equal(Guid.Empty, info.AccessToken);
            Assert.Null(info.CompanyId);
            Assert.Equal("zh-TW", info.Culture);
            Assert.Equal("Asia/Taipei", info.TimeZone);
            Assert.Empty(info.ApiEncryptionKey);
        }

        [Fact]
        [DisplayName("CompanyInfo GetKey 應回傳 CompanyId")]
        public void CompanyInfo_GetKey_ReturnsCompanyId()
        {
            // Arrange
            var info = new CompanyInfo
            {
                CompanyId = "C001",
                CompanyName = "Acme",
                CompanyDatabaseId = "biz_shared_01",
                LogDatabaseId = "log_shared_01"
            };

            // Act & Assert
            Assert.Equal("C001", info.GetKey());
        }

        [Fact]
        [DisplayName("CompanyInfo 預設建構式應產生空字串欄位")]
        public void CompanyInfo_Defaults_ReturnsEmptyStrings()
        {
            // Act
            var info = new CompanyInfo();

            // Assert
            Assert.Equal(string.Empty, info.CompanyId);
            Assert.Equal(string.Empty, info.CompanyName);
            Assert.Equal(string.Empty, info.CompanyDatabaseId);
            Assert.Equal(string.Empty, info.LogDatabaseId);
        }

        [Fact]
        [DisplayName("CompanyInfo ToString 應回傳 CompanyId : CompanyName 格式")]
        public void CompanyInfo_ToString_ReturnsFormattedString()
        {
            // Arrange
            var info = new CompanyInfo { CompanyId = "C001", CompanyName = "Acme" };

            // Act & Assert
            Assert.Equal("C001 : Acme", info.ToString());
        }

        [Fact]
        [DisplayName("SessionInfo ToString 應回傳 UserId : UserName 格式")]
        public void SessionInfo_ToString_ReturnsFormattedString()
        {
            // Arrange
            var info = new SessionInfo { UserId = "U01", UserName = "Alice" };

            // Act & Assert
            Assert.Equal("U01 : Alice", info.ToString());
        }

        [Fact]
        [DisplayName("SessionUser ToString 應回傳 UserID : UserName 格式")]
        public void SessionUser_ToString_ReturnsFormattedString()
        {
            // Arrange
            var user = new SessionUser { UserID = "U02", UserName = "Bob" };

            // Act & Assert
            Assert.Equal("U02 : Bob", user.ToString());
        }

        [Fact]
        [DisplayName("SessionUser 預設值應為空 Guid / MinValue")]
        public void SessionUser_Defaults_ReturnsExpectedValues()
        {
            // Act
            var user = new SessionUser();

            // Assert
            Assert.Equal(Guid.Empty, user.AccessToken);
            Assert.Equal(DateTime.MinValue, user.EndTime);
            Assert.False(user.OneTime);
        }

        [Fact]
        [DisplayName("UserInfo 預設應使用 zh-TW 與 Asia/Taipei")]
        public void UserInfo_Defaults_ReturnsExpectedCultureAndTimeZone()
        {
            // Act
            var user = new UserInfo();

            // Assert
            Assert.Equal("zh-TW", user.Culture);
            Assert.Equal("Asia/Taipei", user.TimeZone);
        }
    }
}
