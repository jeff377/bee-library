using System.ComponentModel;
using System.Text;
using Bee.Api.AspNetCore.Controllers;
using Bee.Api.Core;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.System;

using Bee.Base;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Tests.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bee.Api.AspNetCore.UnitTests
{
    [Collection("Initialize")]
    public class ApiAspNetCoreTests
    {
        private Guid _accessToken;

        static ApiAspNetCoreTests()
        {
        }

        /// <summary>
        /// 測試用的 ApiServiceController 類別。
        /// </summary>
        public class ApiServiceController : Controllers.ApiServiceController { }

        /// <summary>
        /// 取得 JSON-RPC 請求模型的 JSON 字串。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="args">傳入值。</param>
        private static string GetRpcRequestJson(string progId, string action, object args)
        {
            // 設定 JSON-RPC 請求模型
            var request = new JsonRpcRequest()
            {
                Method = $"{progId}.{action}",
                Params = new JsonRpcParams()
                {
                    Value = args
                },
                Id = Guid.NewGuid().ToString()
            };
            return request.ToJson();
        }

        /// <summary>
        /// 執行 ApiServiceController 並傳回反序列化結果。
        /// </summary>
        /// <typeparam name="TResult">回傳型別。</typeparam>
        /// <param name="accessToken">存取權杖。</param>
        /// <param name="progId">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="args">JSON-RPC 傳入參數。</param>
        /// <returns>反序列化後的執行結果。</returns>
        private async Task<TResult> ExecuteRpcAsync<TResult>(Guid accessToken, string progId, string action, object args)
        {
            // 建立 JSON-RPC 請求內容
            string json = GetRpcRequestJson(progId, action, args);

            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var context = new DefaultHttpContext();
            const string apiKey = "valid-api-key";
            var authorization = $"Bearer {accessToken}";
            context.Request.Headers["X-Api-Key"] = apiKey;
            context.Request.Headers["Authorization"] = authorization;
            context.Request.Headers["Content-Type"] = "application/json";
            context.Request.Body = requestBody;

            var controller = new ApiServiceController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };

            // 執行 API
            var result = await controller.PostAsync(apiKey, authorization);
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.False(string.IsNullOrWhiteSpace(contentResult.Content));

            var response = SerializeFunc.JsonToObject<JsonRpcResponse>(contentResult.Content);
            return ApiOutputConverter.ConvertResultValue<TResult>(response!.Result!.Value!)!;
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
        /// 測試 Ping 方法。
        /// </summary>
        [Fact]
        [DisplayName("Ping 應回傳正確狀態與追蹤識別碼")]
        public async Task Ping_ValidRequest_ReturnsOkStatus()
        {
            var args = new PingRequest()
            {
                ClientName = "TestClient",
                TraceId = "001",
            };
            var result = await ExecuteRpcAsync<PingResponse>(Guid.Empty, SysProgIds.System, "Ping", args);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("001", result.TraceId);
        }

        [Fact]
        [DisplayName("ExecFunc 執行 Hello 應回傳非 null 結果")]
        public async Task ExecFunc_Hello_ReturnsNotNull()
        {
            Guid accessToken = GetAccessToken();
            var args = new ExecFuncRequest("Hello");
            var result = await ExecuteRpcAsync<ExecFuncResponse>(accessToken, SysProgIds.System, "ExecFunc", args);
            Assert.NotNull(result);
        }
    }
}
