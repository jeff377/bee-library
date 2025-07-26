using Bee.Base;
using Bee.Cache;
using Bee.Db;
using Bee.Define;

namespace Bee.Business.UnitTests
{
    public class BusinessTest
    {
        /// <summary>
        /// �غc�禡�C
        /// </summary>
        public BusinessTest()
        {
            // �]�w�w�q���|
            BackendInfo.DefinePath = @"D:\DefinePath";
            // ��l�ƪ��_
            var settings = CacheFunc.GetSystemSettings();
            settings.Initialize();
            // ���U��Ʈw���Ѫ�
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // �w�]��Ʈw�s��
            BackendInfo.DatabaseID = "common";
        }

        /// <summary>
        /// �إ߳s�u�C
        /// </summary>
        [Fact]
        public void CreateSession()
        {
            // Arrange
            var business = new SystemBusinessObject();
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
        /// �n�J�t�Ψ����� RSA �[�K���|�ܪ��_�C
        /// </summary>
        [Fact]
        public void Login()
        {
            // Arrange
            // ���� RSA ��٪��_
            RsaCryptor.GenerateRsaKeyPair(out var publicKeyXml, out var privateKeyXml);

            var sbo = new SystemBusinessObject();
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

            // �Ψp�_�ѱK EncryptedSessionKey
            string sessionKey = RsaCryptor.DecryptWithPrivateKey(result.ApiEncryptionKey, privateKeyXml);
            Assert.False(string.IsNullOrWhiteSpace(sessionKey));
        }
    }
}