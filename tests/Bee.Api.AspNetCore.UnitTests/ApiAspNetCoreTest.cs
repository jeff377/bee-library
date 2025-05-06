using System.Text;
using Bee.Api.Core;
using Bee.Base;
using Bee.Db;
using Bee.Define;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bee.Api.AspNetCore.UnitTests
{
    public class ApiAspNetCoreTest
    {
        static ApiAspNetCoreTest()
        {
            // 設定定義路徑
            BackendInfo.DefinePath = @"D:\Bee\src\DefinePath";
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(EDatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        }

        /// <summary>
        /// 測試用的 ApiServiceController 類別。
        /// </summary>
        public class ApiServiceController : TApiServiceController { }

        /// <summary>
        /// 取得 JSON-RPC 請求模型的 JSON 字串。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="args">傳入資料。</param>
        private string GetRpcRequestJson(string progID, string action, object args)
        {
            // 設定 JSON-RPC 請求模型
            var request = new TJsonRpcRequest()
            {
                Method = $"{progID}.{action}",
                Params = new TJsonRpcParams()
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
        /// <param name="progID">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="args">JSON-RPC 傳入參數。</param>
        /// <param name="accessToken">存取權杖。</param>
        /// <returns>反序列化後的執行結果。</returns>
        private async Task<TResult> ExecuteRpcAsync<TResult>(string progID, string action, object args, Guid? accessToken = null)
        {
            accessToken ??= Guid.NewGuid();

            // 建立 JSON-RPC 請求內容
            string json = GetRpcRequestJson(progID, action, args);

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

            var response = SerializeFunc.JsonToObject<TJsonRpcResponse>(contentResult.Content);
            return (TResult)response.Result.Value;
        }

        /// <summary>
        /// 執行 Ping 方法。
        /// </summary>
        [Fact]
        public async Task Ping()
        {
            var args = new TPingArgs()
            {
                ClientName = "TestClient",
                TraceId = "001",
            };
            var result = await ExecuteRpcAsync<TPingResult>(SysProgIDs.System, "Ping", args);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("001", result.TraceId);
        }

        [Fact]
        public async Task Hello()
        {
            var args = new TExecFuncArgs("Hello");
            var result = await ExecuteRpcAsync<TExecFuncResult>(SysProgIDs.System, "ExecFunc", args);
            Assert.NotNull(result);
        }
    }
}