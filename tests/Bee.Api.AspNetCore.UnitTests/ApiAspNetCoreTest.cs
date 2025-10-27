using System.Text;
using Bee.Api.Core;
using Bee.Base;
using Bee.Contracts;
using Bee.Define;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bee.Api.AspNetCore.UnitTests
{
    [Collection("Initialize")]
    public class ApiAspNetCoreTest
    {
        private Guid _accessToken;

        static ApiAspNetCoreTest()
        {
        }

        /// <summary>
        /// 測試用的 ApiServiceController 類別。
        /// </summary>
        public class ApiServiceController : AspNetCore.ApiServiceController { }

        /// <summary>
        /// 取得 JSON-RPC 請求模型的 JSON 字串。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="args">傳入資料。</param>
        private string GetRpcRequestJson(string progId, string action, object args)
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
        /// <param name="accessToken">存取令牌。</param>
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
            context.Request.Headers["X-Api-Key"] = "valid-api-key";
            context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
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
            var result = await controller.PostAsync();
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.False(string.IsNullOrWhiteSpace(contentResult.Content));

            var response = SerializeFunc.JsonToObject<JsonRpcResponse>(contentResult.Content);
            return (TResult)response.Result.Value;
        }

        /// <summary>
        /// 模擬登入並取得 AccessToken。
        /// </summary>
        /// <returns></returns>
        private async Task<Guid> GetAccessTokenAsync()
        {
            if (_accessToken == Guid.Empty)
            {
                // 模擬登入，實際情況應從 API 登入取得 AccessToken
                var args = new LoginArgs()
                {
                    UserId = "demo",    
                    Password = "1234"
                };
                var result = await ExecuteRpcAsync<LoginResult>(Guid.Empty, SysProgIds.System, "Login", args);
                _accessToken = result.AccessToken;
            }
            return _accessToken;
        }

        /// <summary>
        /// 執行 Ping 方法。
        /// </summary>
        [Fact]
        public async Task Ping()
        {
            var args = new PingArgs()
            {
                ClientName = "TestClient",
                TraceId = "001",
            };
            var result = await ExecuteRpcAsync<PingResult>(Guid.Empty, SysProgIds.System, "Ping", args);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("001", result.TraceId);
        }

        [Fact]
        public async Task Hello()
        {
            Guid accessToken = await GetAccessTokenAsync();
            var args = new ExecFuncArgs("Hello");
            var result = await ExecuteRpcAsync<ExecFuncResult>(accessToken, SysProgIds.System, "ExecFunc", args);
            Assert.NotNull(result);
        }
    }
}