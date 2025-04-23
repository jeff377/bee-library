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
        /// <param name="value">傳入資料。</param>
        private string GetRpcRequestJson(string progID, string action, object value)
        {
            // 設定 JSON-RPC 請求模型
            var request = new TJsonRpcRequest()
            {
                Method = $"{progID}.{action}",
                Params = new TJsonRpcParams()
                {
                    Value = value
                },
                Id = Guid.NewGuid()
            };
            return request.ToJson();
        }

        [Fact]
        public async Task Hello()
        {
            // 設定 ExecFunc 方法傳入引數
            Guid accessToken = Guid.NewGuid();
            var args = new TExecFuncArgs("Hello");
            // 取得 JSON-RPC 請求模型的 JSON 字串
            string json =GetRpcRequestJson(SysProgIDs.System, "ExecFunc", args);

            // Arrange
            var controller = new ApiServiceController();
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var context = new DefaultHttpContext();
            context.Request.Headers["X-Api-Key"] = "valid-api-key";
            context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
            context.Request.Body = requestBody;

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = context
            };

            // Act
            var result = await controller.PostAsync();

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
        }
    }
}