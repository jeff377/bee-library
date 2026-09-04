using System.ComponentModel;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bee.Api.AspNetCore.UnitTests
{
    /// <summary>
    /// Tests for error-path branches in <see cref="Controllers.ApiServiceController"/>.
    /// 純錯誤路徑驗證（415/400/401），不依賴 BeeTestFixture / 後端 DI 容器。
    /// </summary>
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
            // 以 ExecFunc 而非 Ping 為測試對象：Ping 已刻意免金鑰（健康檢查在資料庫不可用時
            // 仍須作答），因此不再是「缺金鑰即 401」的樣本。
            const string body = "{\"method\":\"System.ExecFunc\",\"id\":\"1\",\"params\":{}}";
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
    
        [Theory]
        [InlineData(1_000)]        // 遠低於門檻
        [InlineData(40_000)]       // 越過 EnableBuffering 的 30 KB 門檻（先前會落到暫存檔）
        [InlineData(300_000)]
        [DisplayName("大於緩衝門檻的請求主體仍應正確解析（改為直接讀 stream 的迴歸保護）")]
        public async Task PostAsync_LargeBody_ParsesCorrectly(int payloadSize)
        {
            // 這是本次改動的實際風險：從「整份讀成字串再 parse」換成「直接從 stream 反序列化」，
            // 出錯的形態會是部分讀取或編碼問題，而且只在大 body 上顯現。
            var large = await PostAsync("application/json", BuildPingBody(new string('a', payloadSize)));
            var small = await PostAsync("application/json", BuildPingBody("x"));

            // 不變式是「body 大小不得改變結果」。這個裸測試環境沒有後端 DI，兩者都會停在同一個
            // 下游失敗上；重點是大 body 不會因為解析壞掉而變成 400。
            var largeResult = Assert.IsType<ObjectResult>(large);
            var smallResult = Assert.IsType<ObjectResult>(small);

            Assert.NotEqual(StatusCodes.Status400BadRequest, largeResult.StatusCode);
            Assert.Equal(smallResult.StatusCode, largeResult.StatusCode);
        }

        [Fact]
        [DisplayName("多位元組字元跨讀取邊界仍應正確解析")]
        public async Task PostAsync_MultiByteCharactersAcrossBufferBoundary_ParsesCorrectly()
        {
            // 直接從 stream 讀時，UTF-8 的多位元組字元可能落在內部緩衝邊界上。
            // 用大量中文字把邊界撞出來。
            var payload = string.Concat(Enumerable.Repeat("測試字串", 20_000));
            var multiByte = await PostAsync("application/json", BuildPingBody(payload));
            var ascii = await PostAsync("application/json", BuildPingBody("x"));

            var multiByteResult = Assert.IsType<ObjectResult>(multiByte);
            Assert.NotEqual(StatusCodes.Status400BadRequest, multiByteResult.StatusCode);
            Assert.Equal(Assert.IsType<ObjectResult>(ascii).StatusCode, multiByteResult.StatusCode);
        }

        /// <summary>組出一個帶指定字串載荷的合法 JSON-RPC 請求。</summary>
        private static string BuildPingBody(string payload)
            => "{\"id\":\"1\",\"method\":\"System.Ping\",\"params\":{\"value\":\"" + payload + "\"}}";
}
}
