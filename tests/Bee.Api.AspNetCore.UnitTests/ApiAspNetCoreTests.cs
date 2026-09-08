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
using Bee.Api.Core.Conversion;
using Bee.Api.Core.Messages;

namespace Bee.Api.AspNetCore.UnitTests
{
        /// <remarks>
    /// 用 <c>SharedDbFixture</c> 而非 <c>BeeTestFixture</c>：走 controller 的請求會讓 executor
    /// 碰 common 資料庫，<c>BeeTestFixture</c> 不建 schema。
    /// <para>
    /// 本類別驗的是 JSON-RPC 執行本身，金鑰閘門不是它的主題，因此
    /// <see cref="IApiKeyValidator"/> 固定為 <see cref="UnconfiguredApiKeyValidator"/>，
    /// <b>不讀實體金鑰存放處</b>。先前能過只是因為 <c>st_api_key</c> 剛好是空的 ——
    /// 而那不是本類別的任何保證：只要該表存在一列啟用金鑰，閘門就 in force，
    /// <c>"valid-api-key"</c> 不符金鑰格式便成為 <c>ApiKeyStatus.Invalid</c> → 401。
    /// 症狀完全不指向真因，看起來像「預期 200 拿到 401」。
    /// </para>
    /// <para>
    /// 那一列從哪來有兩條路，本機紅 / CI 綠的成因是<b>前者</b>：
    /// (1) <b>殘留</b> —— 本機是持久容器，列會跨測試回合留著；CI 每次都是全新容器，
    /// 該表恆為空，所以同一份程式碼在 CI 上一直是綠的。
    /// (2) <b>平行寫入</b> —— <c>ApiKeyRepositoryTests</c> 會往同一個 common 資料庫寫啟用金鑰，
    /// 正常會在 <c>finally</c> 清掉，但與本專案平行執行時仍有窗口。
    /// （<c>21642741</c> 之後只剩 <c>Insert_ThenGet_SqlServer</c> 一支落在 SQL Server，
    /// 其餘已改打各自 provider，窗口變窄但沒有消失。）
    /// </para>
    /// </remarks>
    public class ApiAspNetCoreTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        private Guid _accessToken;

        public ApiAspNetCoreTests(SharedDbFixture fx)
        {
            _fx = fx;
        }

        /// <summary>
        /// 測試用的 ApiServiceController 類別。
        /// </summary>
        public class ApiServiceController : Controllers.ApiServiceController { }

        /// <summary>
        /// 測試用 <see cref="IHostEnvironment"/>；ApiServiceController.IsDevelopment 會解析此服務。
        /// </summary>
        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Development;
            public string ApplicationName { get; set; } = "Tests";
            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
                = new Microsoft.Extensions.FileProviders.NullFileProvider();
        }

        /// <summary>
        /// 固定回報「此部署尚未發放金鑰」，讓本類別的變動維度只剩 JSON-RPC 執行本身。
        /// 這正是先前依賴實體 <c>st_api_key</c> 為空才成立的狀態，只是現在由測試自己保證。
        /// </summary>
        private sealed class UnconfiguredApiKeyValidator : IApiKeyValidator
        {
            public ApiKeyValidationResult Validate(string? apiKey)
                => new(ApiKeyStatus.NotConfigured);
        }

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
            var context = new DefaultHttpContext
            {
                // Phase 4 後 ApiServiceController 透過 HttpContext.RequestServices 解析
                // JsonRpcExecutor 與 IHostEnvironment；測試使用 TestOverrideServiceProvider 在
                // per-class fixture 的 IServiceProvider 之上補上一個 IHostEnvironment fake。
                RequestServices = new TestOverrideServiceProvider(
                    _fx.Provider,
                    (typeof(IHostEnvironment), new TestHostEnvironment()),
                    (typeof(IApiKeyValidator), new UnconfiguredApiKeyValidator()))
            };
            const string apiKey = "valid-api-key";
            var authorization = $"Bearer {accessToken}";
            context.Request.Headers["X-Api-Key"] = apiKey;
            context.Request.Headers.Authorization = authorization;
            context.Request.Headers.ContentType = "application/json";
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

            var response = JsonCodec.Deserialize<JsonRpcResponse>(contentResult.Content);
            return ApiOutputConverter.ConvertResultValue<TResult>(response!.Result!.Value!)!;
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
