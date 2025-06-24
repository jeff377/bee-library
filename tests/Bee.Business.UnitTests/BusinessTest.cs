using Bee.Db;
using Bee.Define;

namespace Bee.Business.UnitTests
{
    public class BusinessTest
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public BusinessTest()
        {
            // 設定定義路徑
            BackendInfo.DefinePath = @"D:\DefinePath";
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // 預設資料庫編號
            BackendInfo.DatabaseID = "common";
        }

        /// <summary>
        /// 建立連線。
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