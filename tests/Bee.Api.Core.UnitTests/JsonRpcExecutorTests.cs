using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.System;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Tests.Shared;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.UnitTests
{
    [Collection("Initialize")]
    public class JsonRpcExecutorTests
    {
        private Guid _accessToken;

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="accessToken">存取權杖。</param>
        /// <param name="progId">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="value">傳入值。</param>
        private static T ApiExecute<T>(Guid accessToken, string progId, string action, object value)
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
            return (T)response.Result!.Value!;
        }

        /// <summary>
        /// 取得有效的測試 AccessToken（直接在 SessionInfoService 植入，不經過 Login）。
        /// </summary>
        private Guid GetAccessToken()
        {
            if (_accessToken == Guid.Empty)
                _accessToken = TestSessionFactory.CreateAccessToken();
            return _accessToken;
        }

        /// <summary>
        /// 透過 API 執行 Ping 方法。
        /// </summary>
        [Fact]
        [DisplayName("Ping 應回傳正確狀態與追蹤識別碼")]
        public void Ping_ValidRequest_ReturnsOkStatus()
        {
            var args = new PingRequest()
            {
                ClientName = "TestClient",
                TraceId = "001",
            };
            var result = ApiExecute<PingResponse>(Guid.Empty, SysProgIds.System, "Ping", args);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("001", result.TraceId);
        }

        /// <summary>
        /// 測試 GetCommonConfiguration 方法。
        /// </summary>
        [Fact]
        [DisplayName("GetCommonConfiguration 應回傳非 null 結果")]
        public void GetCommonConfiguration_ValidRequest_ReturnsNotNull()
        {
            var args = new GetCommonConfigurationRequest();
            var result = ApiExecute<GetCommonConfigurationResponse>(Guid.Empty, SysProgIds.System, SystemActions.GetCommonConfiguration, args);
            Assert.NotNull(result);
        }

        /// <summary>
        /// 透過 API 執行 Hello 方法。
        /// </summary>
        [Fact]
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
                    Value = new ExecFuncRequest("Hello")
                },
                Id = Guid.NewGuid().ToString()
            };

            string json = request.ToJson();
            // 執行 API 方法
            var executor = new JsonRpcExecutor(accessToken);
            var response = executor.Execute(request);
            // 取得 ExecFunc 方法傳出結果
            var execFuncResult = response.Result!.Value as ExecFuncResponse;
            Assert.NotNull(execFuncResult);  // 確認 ExecFunc 方法傳出結果不為 null
        }
    }
}
