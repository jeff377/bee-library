using System.ComponentModel;
using Bee.Base;
using Bee.Base.Security;
using Bee.Business.System;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    [Collection("Initialize")]
    public class SystemBusinessObjectTests
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public SystemBusinessObjectTests()
        {
        }

        /// <summary>
        /// 建立連線。
        /// </summary>
        [DbFact]
        [DisplayName("CreateSession 傳入有效參數應回傳含 AccessToken 與到期時間的結果")]
        public void CreateSession_ValidArgs_ReturnsTokenWithExpiry()
        {
            // Arrange
            var business = new SystemBusinessObject(Guid.Empty);
            var args = new CreateSessionArgs
            {
                UserID = "001",
                ExpiresIn = 600,
                OneTime = false
            };

            // Act
            var result = business.CreateSession(args);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.AccessToken);
            Assert.True(result.ExpiredAt > DateTime.UtcNow);
        }

        /// <summary>
        /// 登入系統並驗證 RSA 加密金鑰的交換。
        /// </summary>
        // 需要覆寫 SystemBusinessObject.AuthenticateUser（base 實作永遠回傳 false）
        // 才能驗證登入流程；待後續建立測試用子類別再啟用。
#pragma warning disable xUnit1004 // Test methods should not be skipped — placeholder retained as TODO marker; see comment above.
        [Fact(Skip = "Requires a test subclass that overrides AuthenticateUser; not yet in place.")]
#pragma warning restore xUnit1004
        [DisplayName("Login 使用 RSA 金鑰對登入應回傳可解密的加密 Session 金鑰")]
        public void Login_WithRsaKeyPair_ReturnsDecryptableSessionKey()
        {
            // Arrange
            // 產生 RSA 金鑰對
            RsaCryptor.GenerateRsaKeyPair(out var publicKeyXml, out var privateKeyXml);

            var sbo = new SystemBusinessObject(Guid.Empty);
            var args = new LoginArgs
            {
                UserId = "testuser",
                Password = "testpassword",
                ClientPublicKey = publicKeyXml
            };

            // Act
            LoginResult result = sbo.Login(args);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.ApiEncryptionKey);

            // 用私鑰解密 EncryptedSessionKey
            string sessionKey = RsaCryptor.DecryptWithPrivateKey(result.ApiEncryptionKey, privateKeyXml);
            Assert.False(string.IsNullOrWhiteSpace(sessionKey));
        }
    }
}
