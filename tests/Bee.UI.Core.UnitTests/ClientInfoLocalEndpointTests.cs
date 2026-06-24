using System.ComponentModel;
using Bee.Api.Client;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo"/> 中以有效本機端點路徑觸發
    /// <c>InitializeConnectAsync</c> 與 <c>SetEndpointAsync</c> 深層路徑的測試覆蓋率。
    /// <para>
    /// 兩個方法在端點通過 <c>ApiConnectValidator.ValidateAsync</c> 驗證後才會呼叫
    /// <c>SetConnectType</c> 再 <c>await SystemApiConnector.InitializeAsync()</c>；現有測試僅以
    /// 空字串端點觸發驗證拋例外，因此那兩步至今未被執行。本測試以含 <c>SystemSettings.xml</c>
    /// 的暫存目錄通過驗證，覆蓋該兩步（<c>InitializeAsync</c> 在無本機 API 服務時仍會拋例外，
    /// 但 <c>SetConnectType</c> 已先完成；<c>return true;</c> 與 <c>SaveEndpoint</c> 需要正常運行的
    /// API 服務，保留為「無法在 CI 中覆蓋」）。
    /// </para>
    /// 因修改靜態狀態，與其他 ClientInfoState 測試同屬 collection，確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoLocalEndpointTests
    {
        private sealed class FakeEndpointStorage : IEndpointStorage
        {
            private readonly string _endpoint;
            public FakeEndpointStorage(string endpoint) => _endpoint = endpoint;
            public string LoadEndpoint() => _endpoint;
            public void SetEndpoint(string endpoint) { }
            public void SaveEndpoint(string endpoint) { }
        }

        private sealed class FakeUIViewService : IUIViewService
        {
            public Task<bool> ShowApiConnectAsync() => Task.FromResult(false);
        }

        private static string CreateTempDefinePath()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-ci-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            // ApiConnectValidator.ValidateLocal 僅檢查檔案是否存在，不驗證內容
            File.WriteAllText(Path.Combine(tempDir, "SystemSettings.xml"), "<SystemSettings />");
            return tempDir;
        }

        [Fact]
        [DisplayName("InitializeAsync(IUIViewService) 有效本機路徑通過驗證後應將 ConnectType 設為 Local")]
        public async Task InitializeAsync_ValidLocalPath_SetsConnectTypeToLocal()
        {
            var tempDir = CreateTempDefinePath();
            var originalStorage = ClientInfo.EndpointStorage;
            var originalConnectType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ClientInfo.EndpointStorage = new FakeEndpointStorage(tempDir);
                // InitializeConnectAsync 會設定 SupportedConnectTypes = Both，ValidateAsync 通過後呼叫
                // SetConnectType(Local, tempDir) 再 await SystemApiConnector.InitializeAsync()；
                // 無本機 API 服務時 InitializeAsync 拋例外，catch 捕捉並回傳 false。
                await ClientInfo.InitializeAsync(new FakeUIViewService(), SupportedConnectTypes.Both);
                Assert.Equal(ConnectType.Local, ApiClientInfo.ConnectType);
            }
            finally
            {
                ClientInfo.EndpointStorage = originalStorage;
                ApiClientInfo.ConnectType = originalConnectType;
                ApiClientInfo.Endpoint = originalEndpoint;
                ApiClientInfo.SupportedConnectTypes = originalSupportedTypes;
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("SetEndpointAsync 有效本機路徑通過驗證後應將 ConnectType 設為 Local")]
        public async Task SetEndpointAsync_ValidLocalPath_SetsConnectTypeToLocal()
        {
            var tempDir = CreateTempDefinePath();
            var originalConnectType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            try
            {
                // SetEndpointAsync 自身不設定 SupportedConnectTypes，需在呼叫前確保 Local 受支援
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                // ValidateAsync 通過後呼叫 SetConnectType(Local, tempDir)，再 await
                // SystemApiConnector.InitializeAsync()；無本機 API 服務時拋例外向上傳播，SaveEndpoint 不會被執行。
                await Record.ExceptionAsync(() => ClientInfo.SetEndpointAsync(tempDir));
                Assert.Equal(ConnectType.Local, ApiClientInfo.ConnectType);
            }
            finally
            {
                ApiClientInfo.ConnectType = originalConnectType;
                ApiClientInfo.Endpoint = originalEndpoint;
                ApiClientInfo.SupportedConnectTypes = originalSupportedTypes;
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }
    }
}
