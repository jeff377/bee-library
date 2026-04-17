using System.ComponentModel;
using Bee.Business.Validator;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="AccessTokenValidationProvider"/> 行為測試。
    /// </summary>
    [Collection("Initialize")]
    public class AccessTokenValidationProviderTests
    {
        [Fact]
        [DisplayName("ValidateAccessToken(Guid.Empty) 應拋 UnauthorizedAccessException")]
        public void ValidateAccessToken_Empty_ThrowsUnauthorized()
        {
            var provider = new AccessTokenValidationProvider();
            Assert.Throws<UnauthorizedAccessException>(() => provider.ValidateAccessToken(Guid.Empty));
        }

        [Fact]
        [DisplayName("ValidateAccessToken 未知 AccessToken 應拋 UnauthorizedAccessException")]
        public void ValidateAccessToken_UnknownToken_ThrowsUnauthorized()
        {
            var provider = new AccessTokenValidationProvider();
            var token = Guid.NewGuid();

            Assert.Throws<UnauthorizedAccessException>(() => provider.ValidateAccessToken(token));
        }

        [Fact]
        [DisplayName("ValidateAccessToken 過期 Session 應拋 UnauthorizedAccessException")]
        public void ValidateAccessToken_ExpiredSession_ThrowsUnauthorized()
        {
            var provider = new AccessTokenValidationProvider();
            var token = Guid.NewGuid();
            var expired = new SessionInfo
            {
                AccessToken = token,
                UserId = "u01",
                UserName = "U01",
                ExpiredAt = DateTime.UtcNow.AddMinutes(-5),
                ApiEncryptionKey = new byte[64]
            };
            BackendInfo.SessionInfoService.Set(expired);

            try
            {
                Assert.Throws<UnauthorizedAccessException>(() => provider.ValidateAccessToken(token));
            }
            finally
            {
                BackendInfo.SessionInfoService.Remove(token);
            }
        }

        [Fact]
        [DisplayName("ValidateAccessToken 有效 Session 應回傳 true")]
        public void ValidateAccessToken_ValidSession_ReturnsTrue()
        {
            var provider = new AccessTokenValidationProvider();
            var token = Guid.NewGuid();
            var session = new SessionInfo
            {
                AccessToken = token,
                UserId = "u01",
                UserName = "U01",
                ExpiredAt = DateTime.UtcNow.AddHours(1),
                ApiEncryptionKey = new byte[64]
            };
            BackendInfo.SessionInfoService.Set(session);

            try
            {
                Assert.True(provider.ValidateAccessToken(token));
            }
            finally
            {
                BackendInfo.SessionInfoService.Remove(token);
            }
        }
    }
}
