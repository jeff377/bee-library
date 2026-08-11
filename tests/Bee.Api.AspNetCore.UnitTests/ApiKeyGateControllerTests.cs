using System.ComponentModel;
using System.Text;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.System;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Tests.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Bee.Api.AspNetCore.UnitTests
{
    /// <summary>
    /// 控制器對 API 金鑰閘門的接線：驗證器擲例外時必須 fail closed（拒絕），
    /// 而不是被當成「此部署尚未發放金鑰」而放行。
    /// </summary>
        /// <remarks>
    /// 用 <c>SharedDbFixture</c> 而非 <c>BeeTestFixture</c>：走 controller 的每個請求都會經過
    /// <c>ValidateApiKey</c> → <c>ApiKeyValidator.Validate</c> → <c>ApiKeyGate.GetState()</c>，
    /// 那條 read-through 會開 common 連線讀 <c>st_api_key</c>。<c>AddBeeFramework</c> 一律註冊
    /// 真的 <c>ApiKeyValidator</c>，所以這與 access token 無關 —— 這是 <c>rules/testing.md</c>
    /// 未列出的第三條觸發路徑。
    /// <para>
    /// 症狀完全不指向真因：失敗會被 controller 吃掉轉成 <c>ApiKeyStatus.Invalid</c> → 401，
    /// 看起來像「預期 200 拿到 401」。先前之所以是綠的，靠的是 CI 的建 DB 步驟或本機持久容器
    /// 先把 DB 建好，加上 <c>st_api_key</c> 不存在時剛好被 <c>GetTableSchema(...) == null</c> 擋掉
    /// —— 三個都不是這兩個測試自己的保證。
    /// </para>
    /// </remarks>
    public class ApiKeyGateControllerTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public ApiKeyGateControllerTests(SharedDbFixture fx) { _fx = fx; }

        private sealed class TestController : Controllers.ApiServiceController { }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Development;
            public string ApplicationName { get; set; } = "Tests";
            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
                = new Microsoft.Extensions.FileProviders.NullFileProvider();
        }

        /// <summary>
        /// 模擬「金鑰存放處無法讀取」——資料庫不可用時驗證路徑會擲出的形狀。
        /// </summary>
        private sealed class ThrowingApiKeyValidator : IApiKeyValidator
        {
            public ApiKeyValidationResult Validate(string? apiKey)
                => throw new InvalidOperationException("store unreachable");
        }

        private sealed class FixedApiKeyValidator : IApiKeyValidator
        {
            private readonly ApiKeyValidationResult _result;
            public FixedApiKeyValidator(ApiKeyValidationResult result) { _result = result; }
            public ApiKeyValidationResult Validate(string? apiKey) => _result;
        }

        private async Task<IActionResult> PostAsync(string method, IApiKeyValidator? validator, string apiKey)
        {
            // 帶有效 Bearer token，讓唯一的變動維度是 API 金鑰；否則需授權的方法會先因缺
            // Authorization 標頭被拒，測不到金鑰閘門。
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var request = new JsonRpcRequest
            {
                Method = method,
                Params = new JsonRpcParams { Value = new PingRequest { ClientName = "unit", TraceId = "T-1" } },
                Id = Guid.NewGuid().ToString(),
            };

            var overrides = new List<(Type, object?)>
            {
                (typeof(IHostEnvironment), new TestHostEnvironment()),
            };
            if (validator != null)
            {
                overrides.Add((typeof(IApiKeyValidator), validator));
            }

            var context = new DefaultHttpContext
            {
                RequestServices = new TestOverrideServiceProvider(_fx.Provider, overrides.ToArray()),
            };
            context.Request.Headers["X-Api-Key"] = apiKey;
            context.Request.Headers.Authorization = "Bearer " + accessToken;
            context.Request.Headers.ContentType = "application/json";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(request.ToJson()));

            var controller = new TestController
            {
                ControllerContext = new ControllerContext { HttpContext = context },
            };
            return await controller.PostAsync(apiKey, "Bearer " + accessToken);
        }

        [Fact]
        [DisplayName("金鑰驗證擲例外時應 fail closed 回 401，而非降級為寬鬆態")]
        public async Task Post_ValidatorThrows_FailsClosed()
        {
            var result = await PostAsync($"{SysProgIds.System}.GetCommonConfiguration",
                new ThrowingApiKeyValidator(), "any-key");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        }

        [Fact]
        [DisplayName("金鑰驗證擲例外時 System.Ping 仍應作答（健康檢查免金鑰）")]
        public async Task Post_ValidatorThrows_PingStillAnswers()
        {
            var result = await PostAsync($"{SysProgIds.System}.Ping",
                new ThrowingApiKeyValidator(), "any-key");

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            var response = JsonCodec.Deserialize<JsonRpcResponse>(contentResult.Content!);
            Assert.Null(response!.Error);
        }

        [Fact]
        [DisplayName("嚴格態下金鑰無效時應回 401，且訊息不透露拒絕原因")]
        public async Task Post_InvalidKey_ReturnsUnauthorizedWithMergedMessage()
        {
            var validator = new FixedApiKeyValidator(
                new ApiKeyValidationResult(ApiKeyStatus.Invalid, "some-app", string.Empty));

            var result = await PostAsync($"{SysProgIds.System}.GetCommonConfiguration", validator, "some-app.bad");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
            var response = Assert.IsType<JsonRpcResponse>(objectResult.Value);
            Assert.Equal("Missing or invalid API key.", response.Error!.Message);
        }

        [Fact]
        [DisplayName("未註冊 IApiKeyValidator 的 host 應沿用非空檢查（相容態）")]
        public async Task Post_NoValidatorRegistered_UsesPresenceCheck()
        {
            var result = await PostAsync($"{SysProgIds.System}.GetCommonConfiguration", validator: null, apiKey: "any-key");

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
        }

        [Fact]
        [DisplayName("嚴格態下 Ping 帶無效金鑰應回報 Invalid 且不含版本號")]
        public async Task Post_Ping_InvalidKey_ReportsStatusWithoutVersion()
        {
            var validator = new FixedApiKeyValidator(
                new ApiKeyValidationResult(ApiKeyStatus.Invalid, "some-app", string.Empty));

            var result = await PostAsync($"{SysProgIds.System}.Ping", validator, "some-app.bad");

            var contentResult = Assert.IsType<ContentResult>(result);
            var response = JsonCodec.Deserialize<JsonRpcResponse>(contentResult.Content!);
            Assert.Null(response!.Error);
            var ping = Bee.Api.Core.Conversion.ApiOutputConverter
                .ConvertResultValue<PingResponse>(response.Result!.Value!)!;
            Assert.Equal("ok", ping.Status);
            Assert.Equal(ApiKeyStatus.Invalid, ping.ApiKeyStatus);
            Assert.Null(ping.Version);
        }
    }
}
