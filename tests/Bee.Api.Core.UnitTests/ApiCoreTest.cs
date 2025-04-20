using Bee.Db;
using Bee.Define;

namespace Bee.Api.Core.UnitTests
{
    public class ApiCoreTest
    {
        static ApiCoreTest()
        {
            // 設定測試環境
            BackendInfo.DefineProvider = new TFileDefineProvider();
            BackendInfo.BusinessObjectProvider = new Bee.Cache.TBusinessObjectProvider();
            BackendInfo.SystemObject = new Bee.Business.TSystemObject();
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(EDatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        }

        /// <summary>
        /// 透過 API 執行 Hello 方法。
        /// </summary>
        [Fact]
        public void Hello()
        {
            // 設定 ExecFunc 方法傳入引數
            Guid accessToken = Guid.NewGuid();
            var execFuncArgs = new TExecFuncArgs("Hello");
            execFuncArgs.Parameters.Add("Name", "World");
            execFuncArgs.Parameters.Add("Age", 18);
            // 設定 API 方法傳入引數
            var args = new TApiServiceArgs()
            {
                ProgID = SysProgIDs.System,
                Action = "ExecFunc",
                Value = execFuncArgs
            };
            // 執行 API 方法
            var executor = new TApiServiceExecutor(accessToken);
            var result = executor.Execute(args);
            // 取得 ExecFunc 方法傳出結果
            var execFuncResult = result.Value as TExecFuncResult;
            Assert.NotNull(execFuncResult);  // 確認 ExecFunc 方法傳出結果不為 null
        }
    }
}