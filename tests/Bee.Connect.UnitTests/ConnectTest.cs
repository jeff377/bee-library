using Bee.Base;
using Bee.Cache;
using Bee.Db;
using Bee.Define;

namespace Bee.Connect.UnitTests
{
    public class ConnectTest
    {
        static ConnectTest()
        {
            SysInfo.IsDebugMode = true;
            // 設定定義路徑
            BackendInfo.DefinePath = @"D:\DefinePath";
            // 初始化金鑰
            var settings = CacheFunc.GetSystemSettings();
            settings.Initialize();
            // 設定前端 API 金鑰
            FrontendInfo.ApiEncryptionKey = BackendInfo.ApiEncryptionKey;
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // 預設資料庫編號
            BackendInfo.DatabaseID = "common";
            // .NET 8 預設停用 BinaryFormatter，需手動啟用
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
        }

        [Theory]
        [InlineData("http://localhost/jsonrpc/api")]
        //[InlineData("http://localhost/jsonrpc_aspnet/api")]
        public void ApiConnectValidator(string apiUrl)
        {
            var validator = new ApiConnectValidator();
            var connectType = validator.Validate(apiUrl);

            Assert.Equal(ConnectType.Remote, connectType);  // 確認連線方式為遠端連線
        }

        /// <summary>
        /// 透過 SystemConnector 執行 Hello 方法。
        /// </summary>
        [Fact]
        public void SystemConnector_Hello()
        {
            // 設定 ExecFunc 方法傳入引數
            var args = new ExecFuncArgs("Hello");
            // 透過 Connector 執行 ExecFunc 方法
            Guid accessToken = Guid.NewGuid();
            var connector = new SystemApiConnector(accessToken);
            var result = connector.ExecFunc(args);
            Assert.NotNull(result);  // 確認 ExecFunc 方法傳出結果不為 null
        }

        /// <summary>
        /// 透過 FormConnector 執行 Hello 方法。
        /// </summary>
        [Fact]
        public void FormConnector_Hello()
        {
            // 設定 ExecFunc 方法傳入引數
            var args = new ExecFuncArgs("Hello");
            // 透過 Connector 執行 ExecFunc 方法
            Guid accessToken = Guid.NewGuid();
            var connector = new FormApiConnector(accessToken, "demo");
            var result = connector.ExecFunc(args);
            Assert.NotNull(result);  // 確認 ExecFunc 方法傳出結果不為 null
        }

        /// <summary>
        /// 測試 SystemApiConnector 的 CreateSession 方法。
        /// </summary>
        [Fact]
        public void SystemConnector_CreateSession()
        {
            // Arrange
            string userId = "001";
            int expiresIn = 600;
            bool oneTime = false;

            // 產生一個隨機 Guid 作為 accessToken（僅用於初始化，CreateSession 會回傳新的 token）
            Guid accessToken = Guid.NewGuid();
            var connector = new SystemApiConnector(accessToken);

            // Act
            Guid newToken = connector.CreateSession(userId, expiresIn, oneTime);

            // Assert
            Assert.NotEqual(Guid.Empty, newToken); // 應取得有效的 accessToken
        }
    }
}