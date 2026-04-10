using Bee.Base;
using Bee.Base.Security;
using Bee.Api.Contracts;
using Bee.Api.Contracts.System;
using Bee.Business.BusinessObjects;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    [Collection("Initialize")]
    public class BusinessTest
    {
        /// <summary>
        /// �غc�禡�C
        /// </summary>
        public BusinessTest()
        {
        }

        /// <summary>
        /// �إ߳s�u�C
        /// </summary>
        [LocalOnlyFact]
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
        /// �n�J�t�Ψ����� RSA �[�K���|�ܪ��_�C
        /// </summary>
        [LocalOnlyFact]
        public void Login()
        {
            // Arrange
            // ���� RSA ��٪��_
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

            // �Ψp�_�ѱK EncryptedSessionKey
            string sessionKey = RsaCryptor.DecryptWithPrivateKey(result.ApiEncryptionKey, privateKeyXml);
            Assert.False(string.IsNullOrWhiteSpace(sessionKey));
        }
    }
}