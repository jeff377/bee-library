using System.ComponentModel;
using System.Text;
using Bee.Api.Core.JsonRpc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bee.Api.AspNetCore.UnitTests
{
    [Collection("Initialize")]
    public class ApiServiceControllerErrorTests
    {
        public class ApiServiceController : Controllers.ApiServiceController { }

        private static ApiServiceController CreateController(
            string contentType,
            string body,
            string? apiKey = "valid-api-key",
            string? authorization = null)
        {
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var context = new DefaultHttpContext();
            context.Request.ContentType = contentType;
            context.Request.Body = requestBody;

            if (apiKey != null)
                context.Request.Headers["X-Api-Key"] = apiKey;
            if (authorization != null)
                context.Request.Headers.Authorization = authorization;

            return new ApiServiceController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };
        }

        [Fact]
        [DisplayName("PostAsync 非 JSON content-type 應回傳 415 錯誤")]
        public async Task PostAsync_NonJsonContentType_Returns415()
        {
            var controller = CreateController("text/plain", "hello");

            var result = await controller.PostAsync("valid-api-key");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, objectResult.StatusCode);
            var response = Assert.IsType<JsonRpcResponse>(objectResult.Value);
            Assert.NotNull(response.Error);
        }

        [Fact]
        [DisplayName("PostAsync 空白請求 body 應回傳 400 錯誤")]
        public async Task PostAsync_EmptyBody_Returns400()
        {
            var controller = CreateController("application/json", "   ");

            var result = await controller.PostAsync("valid-api-key");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
            var response = Assert.IsType<JsonRpcResponse>(objectResult.Value);
            Assert.NotNull(response.Error);
        }

        [Fact]
        [DisplayName("PostAsync 無效 JSON 格式應回傳 400 ParseError")]
        public async Task PostAsync_InvalidJson_Returns400ParseError()
        {
            var controller = CreateController("application/json", "{not-valid-json}");

            var result = await controller.PostAsync("valid-api-key");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
            var response = Assert.IsType<JsonRpcResponse>(objectResult.Value);
            Assert.NotNull(response.Error);
        }

        [Fact]
        [DisplayName("PostAsync JSON 缺少 method 欄位應回傳 400 InvalidRequest")]
        public async Task PostAsync_MissingMethod_Returns400InvalidRequest()
        {
            var controller = CreateController("application/json",
                "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"params\":{}}");

            var result = await controller.PostAsync("valid-api-key");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
            var response = Assert.IsType<JsonRpcResponse>(objectResult.Value);
            Assert.NotNull(response.Error);
        }

        [Fact]
        [DisplayName("PostAsync 缺少 X-Api-Key 標頭應回傳 401 錯誤")]
        public async Task PostAsync_MissingApiKey_Returns401()
        {
            var body = "{\"jsonrpc\":\"2.0\",\"method\":\"System.Ping\",\"id\":\"1\",\"params\":{\"value\":{}}}";
            var controller = CreateController("application/json", body, apiKey: null);

            var result = await controller.PostAsync(null);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
            var response = Assert.IsType<JsonRpcResponse>(objectResult.Value);
            Assert.NotNull(response.Error);
        }

        [Fact]
        [DisplayName("PostAsync Authorization 格式錯誤應回傳 401 錯誤")]
        public async Task PostAsync_InvalidAuthorizationFormat_Returns401()
        {
            var body = "{\"jsonrpc\":\"2.0\",\"method\":\"System.ExecFunc\",\"id\":\"1\",\"params\":{\"value\":{}}}";
            var controller = CreateController("application/json", body,
                apiKey: "valid-api-key",
                authorization: "NotBearer token");

            var result = await controller.PostAsync("valid-api-key", "NotBearer token");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
            var response = Assert.IsType<JsonRpcResponse>(objectResult.Value);
            Assert.NotNull(response.Error);
        }
    }
}
