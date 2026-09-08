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
    /// 用 <c>SharedDbFixture</c> 而非 <c>BeeTestFixture</c>：這些請求走到 executor 後仍會碰
    /// common 資料庫，<c>BeeTestFixture</c> 不建 schema。
    /// <para>
    /// 金鑰驗證這一維由每個測試自己指定的 <see cref="IApiKeyValidator"/> 決定，
    /// <b>不讀實體金鑰存放處</b>。這是刻意的：一旦讓本類別去讀 <c>st_api_key</c>，
    /// 閘門是否 in force 就取決於該表當下有沒有列，而那不是本類別的任何保證。
    /// 症狀還完全不指向真因：閘門 in force 時任何非金鑰格式的標頭都成為
    /// <c>ApiKeyStatus.Invalid</c> → 401，看起來像「預期 200 拿到 401」。
    /// </para>
    /// <para>
    /// 那一列從哪來有兩條路，本機紅 / CI 綠的成因是<b>前者</b>：
    /// (1) <b>殘留</b> —— 本機是持久容器，列會跨測試回合留著；CI 每次都是全新容器，
    /// 該表恆為空。(2) <b>平行寫入</b> —— <c>ApiKeyRepositoryTests</c> 會往同一個 common
    /// 資料庫寫啟用金鑰，正常會在 <c>finally</c> 清掉，但與本專案平行執行時仍有窗口。
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

            // IMPORTANT: register the IApiKeyValidator override unconditionally, `null` included.
            // TestOverrideServiceProvider honours a null instance by short-circuiting the inner
            // provider, and that is the only way to reach the no-validator path: AddBeeFramework
            // always registers a real ApiKeyValidator, so adding the override only when non-null
            // let the lookup fall through to it. Post_NoValidatorRegistered_UsesPresenceCheck then
            // exercised the live key store rather than the compatibility path it names, and its
            // verdict depended on whether the shared common database happened to hold an enabled key.
            var overrides = new (Type, object?)[]
            {
                (typeof(IHostEnvironment), new TestHostEnvironment()),
                (typeof(IApiKeyValidator), validator),
            };

            var context = new DefaultHttpContext
            {
                RequestServices = new TestOverrideServiceProvider(_fx.Provider, overrides),
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
