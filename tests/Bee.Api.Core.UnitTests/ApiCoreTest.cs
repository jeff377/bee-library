using System.ComponentModel;
using Bee.Api.Core;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Api.Contracts;
using Bee.Api.Contracts.System;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests
{
    [Collection("Initialize")]
    public class ApiCoreTest
    {
        private Guid _accessToken;

        static ApiCoreTest()
        {

        }

        /// <summary>
        /// JSON-RPC 請求模型序列化。
        /// </summary>
        [Fact]
        [DisplayName("JsonRpcRequest 序列化應產生有效 JSON 並支援編碼與解碼")]
        public void JsonRpcRequest_Serialize_ReturnsValidJson()
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
        /// <param name="accessToken">存取權杖。</param>
        /// <param name="progId">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="value">傳入值。</param>
        private T ApiExecute<T>(Guid accessToken, string progId, string action, object value)
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

            var executor = new JsonRpcExecutor(accessToken);
            var response = executor.Execute(request);
            return (T)response.Result.Value;
        }

        /// <summary>
        /// 模擬登入並取得 AccessToken。
        /// </summary>
        /// <returns></returns>
        private Guid GetAccessToken()
        {
            if (_accessToken == Guid.Empty)
            {
                // 模擬登入，實際上是透過 API 登入取得 AccessToken
                var args = new LoginArgs()
                {
                    UserId = "demo",
                    Password = "1234"
                };
                var result = ApiExecute<LoginResult>(Guid.Empty, SysProgIds.System, "Login", args);
                _accessToken = result.AccessToken;
            }
            return _accessToken;
        }

        /// <summary>
        /// 透過 API 執行 Ping 方法。
        /// </summary>
        [LocalOnlyFact]
        [DisplayName("Ping 應回傳正確狀態與追蹤識別碼")]
        public void Ping_ValidRequest_ReturnsOkStatus()
        {
            var args = new PingArgs()
            {
                ClientName = "TestClient",
                TraceId = "001",
            };
            var result = ApiExecute<PingResult>(Guid.Empty, SysProgIds.System, "Ping", args);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("001", result.TraceId);
        }

        /// <summary>
        /// 測試 GetCommonConfiguration 方法。
        /// </summary>
        [LocalOnlyFact]
        [DisplayName("GetCommonConfiguration 應回傳非 null 結果")]
        public void GetCommonConfiguration_ValidRequest_ReturnsNotNull()
        {
            var args = new GetCommonConfigurationArgs();
            var result = ApiExecute<GetCommonConfigurationResult>(Guid.Empty, SysProgIds.System, SystemActions.GetCommonConfiguration, args);
            Assert.NotNull(result);
            //Assert.Equal("messagepack", result.Serializer);
        }

        /// <summary>
        /// 透過 API 執行 Hello 方法。
        /// </summary>
        [LocalOnlyFact]
        [DisplayName("ExecFunc 執行 Hello 應回傳非 null 結果")]
        public void ExecFunc_Hello_ReturnsNotNull()
        {
            // 取得 AccessToken
            Guid accessToken = GetAccessToken();

            // 設定 JSON-RPC 請求模型
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

    }
}
