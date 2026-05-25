using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.System;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Tests.Shared;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.UnitTests
{
    public class JsonRpcExecutorTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;
        private Guid _accessToken;

        public JsonRpcExecutorTests(BeeTestFixture fx)
        {
            _fx = fx;
        }

        private JsonRpcExecutor NewExecutor(Guid accessToken, bool isLocalCall = true)
        {
            var executor = new JsonRpcExecutor(
                _fx.GetRequiredService<IBusinessObjectFactory>(),
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = accessToken,
                IsLocalCall = isLocalCall,
            };
            return executor;
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

            var executor = NewExecutor(accessToken);
            var response = executor.Execute(request);
            return (T)response.Result!.Value!;
        }

        /// <summary>
        /// 取得有效的測試 AccessToken（直接在 SessionInfoService 植入，不經過 Login）。
        /// </summary>
        private Guid GetAccessToken()
        {
            if (_accessToken == Guid.Empty)
                _accessToken = TestSessionFactory.CreateAccessToken(_fx);
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
        /// 模擬 JS 前端送 Plain 格式但完全省略 params.type 欄位，
        /// 驗證 server 仍能透過 BO 方法 reflection 拿到目標型別並正確反序列化。
        /// </summary>
        [Fact]
        [DisplayName("Plain 格式不帶 type 欄位 server 應正常反序列化")]
        public void Ping_PlainWithoutTypeField_DeserializesAndReturnsOk()
        {
            // params 內完全沒有 "type" 欄位 — 模擬 JS 原生送出的 JSON
            const string json = """
                {
                    "jsonrpc": "2.0",
                    "method": "System.Ping",
                    "params": {
                        "format": 0,
                        "value": { "clientName": "JsClient", "traceId": "js-001" }
                    },
                    "id": "js-req-1"
                }
                """;

            var request = JsonCodec.Deserialize<JsonRpcRequest>(json);
            Assert.NotNull(request);
            Assert.Equal(PayloadFormat.Plain, request.Params.Format);
            Assert.Equal(string.Empty, request.Params.TypeName); // type 未送 → 預設空字串

            var executor = NewExecutor(Guid.Empty);
            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = response.Result!.Value as PingResponse;
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("js-001", result.TraceId);
        }

        /// <summary>
        /// 模擬 JS 前端送 Plain 格式且 params.type 為空字串，
        /// 驗證 server 行為與「完全省略 type」一致。
        /// </summary>
        [Fact]
        [DisplayName("Plain 格式 type 為空字串 server 應正常反序列化")]
        public void Ping_PlainWithEmptyTypeField_DeserializesAndReturnsOk()
        {
            const string json = """
                {
                    "jsonrpc": "2.0",
                    "method": "System.Ping",
                    "params": {
                        "format": 0,
                        "value": { "clientName": "JsClient", "traceId": "js-002" },
                        "type": ""
                    },
                    "id": "js-req-2"
                }
                """;

            var request = JsonCodec.Deserialize<JsonRpcRequest>(json);
            Assert.NotNull(request);

            var executor = NewExecutor(Guid.Empty);
            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = response.Result!.Value as PingResponse;
            Assert.NotNull(result);
            Assert.Equal("js-002", result.TraceId);
        }

        /// <summary>
        /// 模擬 JS 前端送 Plain 格式且 params.type 帶錯誤型別字串，
        /// 驗證 Plain 路徑完全不讀 type 欄位 (RestoreFrom 在 Plain 分支前 early-return)。
        /// </summary>
        [Fact]
        [DisplayName("Plain 格式 type 帶錯誤型別字串 server 應忽略並正常反序列化")]
        public void Ping_PlainWithBogusTypeField_IgnoresTypeAndReturnsOk()
        {
            const string json = """
                {
                    "jsonrpc": "2.0",
                    "method": "System.Ping",
                    "params": {
                        "format": 0,
                        "value": { "clientName": "JsClient", "traceId": "js-003" },
                        "type": "NonExistent.Type.That.Should.Be.Ignored, FakeAssembly"
                    },
                    "id": "js-req-3"
                }
                """;

            var request = JsonCodec.Deserialize<JsonRpcRequest>(json);
            Assert.NotNull(request);
            Assert.Equal("NonExistent.Type.That.Should.Be.Ignored, FakeAssembly", request.Params.TypeName);

            var executor = NewExecutor(Guid.Empty);
            var response = executor.Execute(request);

            // 若 Plain 路徑讀了 type，這裡會炸 (whitelist 拒絕 / 無法載入型別)；
            // 不炸代表框架完全忽略 type，靠 BO 方法 reflection 拿到 PingRequest。
            Assert.Null(response.Error);
            var result = response.Result!.Value as PingResponse;
            Assert.NotNull(result);
            Assert.Equal("js-003", result.TraceId);
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

            _ = request.ToJson();
            // 執行 API 方法
            var executor = NewExecutor(accessToken);
            var response = executor.Execute(request);
            // 取得 ExecFunc 方法傳出結果
            var execFuncResult = response.Result!.Value as ExecFuncResponse;
            Assert.NotNull(execFuncResult);  // 確認 ExecFunc 方法傳出結果不為 null
        }
    }
}
