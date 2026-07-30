using System.ComponentModel;
using Bee.Base.Security;
using Bee.Business.System;
using Bee.Tests.Shared;
using Bee.Definition.Database;
using Bee.Definition.Identity;

namespace Bee.Business.UnitTests
{
    public class SystemBusinessObjectTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectTests(SharedDbFixture fx) { _fx = fx; }
        /// <summary>
        /// 建立連線。
        /// </summary>
        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("CreateSession 傳入有效參數應回傳含 AccessToken 與到期時間的結果")]
        public void CreateSession_ValidArgs_ReturnsTokenWithExpiry()
        {
            // Arrange
            var business = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);
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

            // 走的是與 Login 相同的建構路徑：解析使用者名稱、套語系、產生金鑰、寫種子、寫快取。
            // 先前只做一次 raw INSERT，取得的 token 在快取中找不到 session，等同不可用。
            var session = _fx.GetRequiredService<ISessionInfoService>().Get(result.AccessToken);
            try
            {
                Assert.NotNull(session);
                Assert.Equal("001", session!.UserId);
                Assert.NotEmpty(session.UserName);
                Assert.NotEmpty(session.ApiEncryptionKey);
                Assert.NotEmpty(session.Culture);
            }
            finally
            {
                _fx.GetRequiredService<ISessionInfoService>().Remove(result.AccessToken);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("CreateSession 傳入不存在的使用者編號應擲 InvalidOperationException")]
        public void CreateSession_NonExistentUserId_ThrowsInvalidOperation()
        {
            var business = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);
            var args = new CreateSessionArgs { UserID = "__nonexistent_user_xyz__", ExpiresIn = 600 };

            Assert.Throws<InvalidOperationException>(() => business.CreateSession(args));
        }

        [Fact]
        [DisplayName("CreateSession 要求一次性 token 應擲 NotSupportedException 而非靜默降級")]
        public void CreateSession_OneTime_ThrowsNotSupported()
        {
            var business = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);
            var args = new CreateSessionArgs { UserID = "001", ExpiresIn = 600, OneTime = true };

            // 建立時即寫入快取後，第一次使用是 cache hit，delete-on-read 永遠不會觸發，
            // 一次性語意無處消費。讓帶安全意味的保證無聲失效是最差的選項，故明確拒絕。
            Assert.Throws<NotSupportedException>(() => business.CreateSession(args));
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
            RsaCryptor.GenerateRsaKeyPair(out var publicKey, out var privateKey);

            var sbo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);
            var args = new LoginArgs
            {
                UserId = "testuser",
                Password = "testpassword",
                ClientPublicKey = publicKey
            };

            // Act
            LoginResult result = sbo.Login(args);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.ApiEncryptionKey);

            // 用私鑰解密 EncryptedSessionKey
            string sessionKey = RsaCryptor.DecryptWithPrivateKey(result.ApiEncryptionKey, privateKey);
            Assert.False(string.IsNullOrWhiteSpace(sessionKey));
        }
    }
}
