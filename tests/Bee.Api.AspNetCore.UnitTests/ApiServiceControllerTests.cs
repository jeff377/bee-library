using System.ComponentModel;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bee.Api.AspNetCore.UnitTests
{
    /// <summary>
    /// Tests for error-path branches in <see cref="Controllers.ApiServiceController"/>.
    /// </summary>
    [Collection("Initialize")]
    public class ApiServiceControllerTests
    {
        private sealed class TestController : Controllers.ApiServiceController { }

        private static async Task<IActionResult> PostAsync(
            string contentType,
            string body,
            string? apiKey = "valid-api-key",
            string? authorization = null)
        {
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Api-Key"] = apiKey ?? string.Empty;
            if (authorization != null)
                context.Request.Headers.Authorization = authorization;
            context.Request.Headers.ContentType = contentType;
            context.Request.Body = requestBody;

            var controller = new TestController
            {
                ControllerContext = new ControllerContext { HttpContext = context }
            };

            return await controller.PostAsync(apiKey, authorization);
        }

        [Fact]
        [DisplayName("PostAsync 非 application/json Content-Type 應回傳 415")]
        public async Task PostAsync_WrongContentType_Returns415()
        {
            var result = await PostAsync("text/plain", "{}");

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, obj.StatusCode);
        }

        [Fact]
        [DisplayName("PostAsync Content-Type 缺少時應回傳 415")]
        public async Task PostAsync_NullContentType_Returns415()
        {
            var result = await PostAsync("", "{}");

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, obj.StatusCode);
        }

        [Fact]
        [DisplayName("PostAsync 空請求主體應回傳 400")]
        public async Task PostAsync_EmptyBody_Returns400()
        {
            var result = await PostAsync("application/json", "   ");

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
        }

        [Fact]
        [DisplayName("PostAsync 無效 JSON 應回傳 400 ParseError")]
        public async Task PostAsync_InvalidJson_Returns400ParseError()
        {
            var result = await PostAsync("application/json", "not-valid-json{{{");

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
        }

        [Fact]
        [DisplayName("PostAsync JSON 缺少 method 欄位應回傳 400")]
        public async Task PostAsync_MissingMethod_Returns400()
        {
            var result = await PostAsync("application/json", "{\"id\":\"1\",\"params\":{}}");

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
        }

        [Fact]
        [DisplayName("PostAsync 無 API 金鑰應回傳 401")]
        public async Task PostAsync_MissingApiKey_Returns401()
        {
            const string body = "{\"method\":\"System.Ping\",\"id\":\"1\",\"params\":{\"clientName\":\"t\",\"traceId\":\"0\"}}";
            var result = await PostAsync("application/json", body, apiKey: null);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, obj.StatusCode);
        }

        [Fact]
        [DisplayName("PostAsync 需要認證的方法但無 Authorization 標頭應回傳 401")]
        public async Task PostAsync_AuthRequiredButNoAuthorization_Returns401()
        {
            const string body = "{\"method\":\"System.ExecFunc\",\"id\":\"1\",\"params\":{}}";
            var result = await PostAsync("application/json", body, apiKey: "valid-api-key");

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, obj.StatusCode);
        }
    }
}
