using System.ComponentModel;
using Bee.Business.Validator;
using Bee.Definition;
using Bee.Definition.Identity;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="AccessTokenValidator"/> 行為測試。
    /// </summary>
    [Collection("Initialize")]
    public class AccessTokenValidatorTests
    {
        [Fact]
        [DisplayName("Validate(Guid.Empty) 應拋 UnauthorizedAccessException")]
        public void Validate_Empty_ThrowsUnauthorized()
        {
            var provider = new AccessTokenValidator();
            Assert.Throws<UnauthorizedAccessException>(() => provider.Validate(Guid.Empty));
        }

        [Fact]
        [DisplayName("Validate 未知 AccessToken 應拋 UnauthorizedAccessException")]
        public void Validate_UnknownToken_ThrowsUnauthorized()
        {
            var provider = new AccessTokenValidator();
            var token = Guid.NewGuid();

            Assert.Throws<UnauthorizedAccessException>(() => provider.Validate(token));
        }

        [Fact]
        [DisplayName("Validate 過期 Session 應拋 UnauthorizedAccessException")]
        public void Validate_ExpiredSession_ThrowsUnauthorized()
        {
            var provider = new AccessTokenValidator();
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
                Assert.Throws<UnauthorizedAccessException>(() => provider.Validate(token));
            }
            finally
            {
                BackendInfo.SessionInfoService.Remove(token);
            }
        }

        [Fact]
        [DisplayName("Validate 有效 Session 應回傳 true")]
        public void Validate_ValidSession_ReturnsTrue()
        {
            var provider = new AccessTokenValidator();
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
                Assert.True(provider.Validate(token));
            }
            finally
            {
                BackendInfo.SessionInfoService.Remove(token);
            }
        }
    }
}
