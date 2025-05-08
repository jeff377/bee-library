using Bee.Db;
using Bee.Define;

namespace Bee.Connect.UnitTests
{
    public class ConnectTest
    {
        static ConnectTest()
        {
            // 設定定義路徑
            BackendInfo.DefinePath = @"D:\Bee\src\DefinePath";
            // 設定測試環境
            BackendInfo.DefineProvider = new TFileDefineProvider();
            BackendInfo.BusinessObjectProvider = new Bee.Cache.TBusinessObjectProvider();
            BackendInfo.SystemObject = new Bee.Business.TSystemObject();
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(EDatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // .NET 8 預設停用 BinaryFormatter，需手動啟用
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
        }

        [Fact]
        public void ApiConnectValidator()
        {
            var validator = new TApiConnectValidator();
            var connectType = validator.Validate("http://localhost/jsonrpc_aspnet/api");
            Assert.Equal(EConnectType.Remote, connectType);  // 確認連線方式為遠端連線
        }

        /// <summary>
        /// 透過 Connect 執行 Hello 方法。
        /// </summary>
        [Fact]
        public void Hello()
        {
            // 設定 ExecFunc 方法傳入引數
            var args = new TExecFuncArgs("Hello");
            // 透過 Connect 執行 ExecFunc 方法
            Guid accessToken = Guid.NewGuid();
            var connector = new TSystemConnector(accessToken);
            var result = connector.ExecFunc(args);
            Assert.NotNull(result);  // 確認 ExecFunc 方法傳出結果不為 null
        }
    }
}