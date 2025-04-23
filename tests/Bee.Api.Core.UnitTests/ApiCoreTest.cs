using Bee.Base;
using Bee.Db;
using Bee.Define;

namespace Bee.Api.Core.UnitTests
{
    public class ApiCoreTest
    {
        static ApiCoreTest()
        {
            // 設定定義路徑
            BackendInfo.DefinePath = @"D:\Bee\src\DefinePath";
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(EDatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        }

        /// <summary>
        /// JSON-RPC 請求模型序列化。
        /// </summary>
        [Fact]
        public void JsonRpcRequestSerialize()
        {
            var request = new TJsonRpcRequest()
            {
                Method = $"{SysProgIDs.System}.ExecFunc",
                Params = new TJsonRpcParams()
                {
                    Value = new TExecFuncArgs("Hello")
                },
                Id = Guid.NewGuid().ToString()
            };
            string json = request.ToJson();
            Assert.NotNull(json);
        }

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="value">傳入資料。</param>
        private T ApiExecute<T>(string progID, string action, object value)
        {
            // 設定 JSON-RPC 請求模型
            var request = new TJsonRpcRequest()
            {
                Method = $"{progID}.{action}",
                Params = new TJsonRpcParams()
                {
                    Value = value
                },
                Id = Guid.NewGuid().ToString()
            };
            Guid accessToken = Guid.NewGuid();
            var executor = new TJsonRpcExecutor(accessToken);
            var response = executor.Execute(request);
            return (T)response.Result.Value;
        }

        /// <summary>
        /// 透過 API 執行 Hello 方法。
        /// </summary>
        [Fact]
        public void Hello()
        {
            // 設定 ExecFunc 方法傳入引數
            Guid accessToken = Guid.NewGuid();
            // 設定 設定 JSON-RPC 請求模型
            var request = new TJsonRpcRequest()
            {
                Method = $"{SysProgIDs.System}.ExecFunc",
                Params = new TJsonRpcParams()
                {
                    Value = new TExecFuncArgs("Hello")
                },
                Id = Guid.NewGuid().ToString()
            };

            string json = request.ToJson();
            // 執行 API 方法
            var executor = new TJsonRpcExecutor(accessToken);
            var response = executor.Execute(request);
            // 取得 ExecFunc 方法傳出結果
            var execFuncResult = response.Result.Value as TExecFuncResult;
            Assert.NotNull(execFuncResult);  // 確認 ExecFunc 方法傳出結果不為 null
        }

        [Fact]
        public void TestDatabaseId()
        {
            var args = new TExecFuncArgs("TestDatabaseId");
            args.Parameters.Add("DatabaseId", "common");
            var result = ApiExecute<TExecFuncResult>(SysProgIDs.System, "ExecFunc", args);
            Assert.NotNull(result);
        }
    }
}