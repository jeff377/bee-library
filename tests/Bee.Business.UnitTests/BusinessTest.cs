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
            Assert.True(result.Expires > DateTime.Now);
        }
    }
}