using Bee.Base;
using Bee.Cache;
using Bee.Db;
using Bee.Define;

namespace Bee.Api.Core.UnitTests
{
    [Collection("Initialize")]
    public class ApiCoreTest
    {
        static ApiCoreTest()
        {

        }

        /// <summary>
        /// JSON-RPC 請求模型序列化。
        /// </summary>
        [Fact]
        public void JsonRpcRequestSerialize()
        {
            var request = new JsonRpcRequest()
            {
                Method = $"{SysProgIds.System}.ExecFunc",
                Params = new JsonRpcParams()
                {
                    Value = new ExecFuncArgs("Hello")
                },
                Id = Guid.NewGuid().ToString()
            };
            string json = request.ToJson();
            Assert.NotEmpty(json);

            // 測試編碼
            ApiPayloadConverter.TransformTo(request.Params, PayloadFormat.Encoded);
            string encodedJson = request.ToJson();
            Assert.NotEmpty(encodedJson);

            // 測試解碼
            ApiPayloadConverter.RestoreFrom(request.Params, PayloadFormat.Encoded);
            string decodedJson = request.ToJson();
            Assert.NotEmpty(decodedJson);
        }

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="value">傳入資料。</param>
        private T ApiExecute<T>(string progId, string action, object value)
        {
            // 設定 JSON-RPC 請求模型
            var request = new JsonRpcRequest()
            {
                Method = $"{progId}.{action}",
                Params = new JsonRpcParams()
                {
                    Value = value
                },
                Id = Guid.NewGuid().ToString()
            };
            Guid accessToken = Guid.NewGuid();
            var executor = new JsonRpcExecutor(accessToken);
            var response = executor.Execute(request);
            return (T)response.Result.Value;
        }

        /// <summary>
        /// 透過 API 執行 Ping 方法。
        /// </summary>
        [Fact]
        public void Ping()
        {
            var args = new PingArgs()
            {
                ClientName = "TestClient",
                TraceId = "001",
            };
            var result = ApiExecute<PingResult>(SysProgIds.System, "Ping", args);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("001", result.TraceId);
        }

        /// <summary>
        /// 執行 GetCommonConfiguration 方法。
        /// </summary>
        [Fact]
        public void GetCommonConfiguration()
        {
            var args = new GetCommonConfigurationArgs();
            var result = ApiExecute<GetCommonConfigurationResult>(SysProgIds.System, SystemActions.GetCommonConfiguration, args);
            Assert.NotNull(result);
            //Assert.Equal("messagepack", result.Serializer);
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
            var request = new JsonRpcRequest()
            {
                Method = $"{SysProgIds.System}.ExecFunc",
                Params = new JsonRpcParams()
                {
                    Value = new ExecFuncArgs("Hello")
                },
                Id = Guid.NewGuid().ToString()
            };

            string json = request.ToJson();
            // 執行 API 方法
            var executor = new JsonRpcExecutor(accessToken);
            var response = executor.Execute(request);
            // 取得 ExecFunc 方法傳出結果
            var execFuncResult = response.Result.Value as ExecFuncResult;
            Assert.NotNull(execFuncResult);  // 確認 ExecFunc 方法傳出結果不為 null
        }

        [Fact]
        public void TestDatabaseId()
        {
            var args = new ExecFuncArgs("TestDatabaseId");
            args.Parameters.Add("DatabaseId", "common");
            var result = ApiExecute<ExecFuncResult>(SysProgIds.System, "ExecFunc", args);
            Assert.NotNull(result);
        }
    }
}