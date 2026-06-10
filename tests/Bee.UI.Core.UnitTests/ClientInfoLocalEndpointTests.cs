using System.ComponentModel;
using Bee.Api.Client;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo"/> 中以有效本機端點路徑觸發
    /// <c>InitializeConnect</c> 與 <c>SetEndpoint</c> 深層路徑的測試覆蓋率。
    /// <para>
    /// 兩個方法在端點通過 <c>ApiConnectValidator.Validate</c> 驗證後才會呼叫
    /// <c>SetConnectType</c> 與 <c>SyncExecutor.Run</c>；現有測試僅以空字串端點
    /// 觸發驗證拋例外，因此那兩行至今未被執行。本測試以含 <c>SystemSettings.xml</c>
    /// 的暫存目錄通過驗證，覆蓋該兩行（<c>SyncExecutor.Run</c> 在無本機 API 服務
    /// 時仍會拋例外，但執行本身已完成；<c>return true;</c> 與 <c>SaveEndpoint</c>
    /// 需要正常運行的 API 服務，保留為「無法在 CI 中覆蓋」）。
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
            public bool ShowApiConnect() => false;
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
        [DisplayName("Initialize(IUIViewService) 有效本機路徑通過驗證後應將 ConnectType 設為 Local")]
        public void Initialize_ValidLocalPath_SetsConnectTypeToLocal()
        {
            var tempDir = CreateTempDefinePath();
            var originalStorage = ClientInfo.EndpointStorage;
            var originalConnectType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ClientInfo.EndpointStorage = new FakeEndpointStorage(tempDir);
                // InitializeConnect 會設定 SupportedConnectTypes = Both，Validate 通過後呼叫
                // SetConnectType(Local, tempDir) 再呼叫 SyncExecutor.Run；
                // 無本機 API 服務時 Run 拋例外，catch 捕捉並回傳 false。
                ClientInfo.Initialize(new FakeUIViewService(), SupportedConnectTypes.Both);
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
        [DisplayName("Initialize(string) 有效本機路徑通過驗證後應將 ConnectType 設為 Local")]
        public void Initialize_StringEndpoint_ValidLocalPath_SetsConnectTypeToLocal()
        {
            var tempDir = CreateTempDefinePath();
            var originalConnectType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                // Initialize(string) delegates to SetEndpoint which validates + calls
                // SetConnectType; no local API service → SyncExecutor.Run throws.
                Record.Exception(() => ClientInfo.Initialize(tempDir));
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

        [Fact]
        [DisplayName("SetEndpoint 有效本機路徑通過驗證後應將 ConnectType 設為 Local")]
        public void SetEndpoint_ValidLocalPath_SetsConnectTypeToLocal()
        {
            var tempDir = CreateTempDefinePath();
            var originalConnectType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            try
            {
                // SetEndpoint 自身不設定 SupportedConnectTypes，需在呼叫前確保 Local 受支援
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                // Validate 通過後呼叫 SetConnectType(Local, tempDir)，再呼叫 SyncExecutor.Run；
                // 無本機 API 服務時 Run 拋例外向上傳播，SaveEndpoint 不會被執行。
                Record.Exception(() => ClientInfo.SetEndpoint(tempDir));
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
