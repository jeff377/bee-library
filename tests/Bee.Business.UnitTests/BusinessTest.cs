using Bee.Base;
using Bee.Cache;
using Bee.Db;
using Bee.Define;

namespace Bee.Business.UnitTests
{
    [Collection("Initialize")]
    public class BusinessTest
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public BusinessTest()
        {
        }

        /// <summary>
        /// 建立連線。
        /// </summary>
        [Fact]
        public void CreateSession()
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
        /// 登入系統並驗證 RSA 加密的會話金鑰。
        /// </summary>
        [Fact]
        public void Login()
        {
            // Arrange
            // 產生 RSA 對稱金鑰
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