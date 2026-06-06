using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo"/> 中以有效遠端 URL 觸發 <c>SetEndpoint</c>（line 177–178）
    /// 與 <c>InitializeConnect</c>（line 197–198）深層路徑的測試覆蓋率。
    /// <para>
    /// <c>FileUtilities.IsLocalPath</c> 僅匹配 Windows 路徑 pattern（<c>^[a-zA-Z]:\</c>），
    /// 使 Linux CI 上 <c>/tmp/…</c> 無法通過本機路徑驗證，導致既有的本機路徑測試在 Linux 上
    /// 於 <c>ApiConnectValidator.Validate</c> 提早拋例外，造成 line 177–178 與 197–198
    /// 從未被執行。改以遠端 URL（<c>http://localhost:19999</c>）：<c>ValidateRemote</c> 僅
    /// 驗格式、不發實際 HTTP 請求；<c>SetConnectType</c>（line 177/197）與
    /// <c>SyncExecutor.Run</c>（line 178/198）均會執行，後者因 localhost:19999 無伺服器
    /// 監聽而快速拋 <c>HttpRequestException</c>（connection refused）。
    /// </para>
    /// 因修改靜態狀態，與其他 ClientInfoState 測試同屬 collection，確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoRemoteEndpointTests
    {
        private static readonly FieldInfo s_sysConnField =
            typeof(ClientInfo).GetField("_systemConnector", BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly PropertyInfo s_uiViewServiceProp =
            typeof(ClientInfo).GetProperty("UIViewService", BindingFlags.Public | BindingFlags.Static)!;

        private static readonly PropertyInfo s_argumentsProp =
            typeof(ClientInfo).GetProperty("Arguments", BindingFlags.Public | BindingFlags.Static)!;

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
            private readonly bool _result;
            public FakeUIViewService(bool result) => _result = result;
            public bool ShowApiConnect() => _result;
        }

        [Fact]
        [DisplayName("SetEndpoint 有效遠端 URL 應在 SyncExecutor.Run 拋例外前執行 SetConnectType（覆蓋 line 177–178）")]
        public void SetEndpoint_ValidRemoteUrl_SetsConnectTypeBeforeThrow()
        {
            var originalType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            var originalSysConn = s_sysConnField.GetValue(null);
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                // ValidateRemote 僅驗格式不發 HTTP；SetConnectType 在 SyncExecutor.Run 前執行後者拋例外
                Record.Exception(() => ClientInfo.SetEndpoint("http://localhost:19999"));
                Assert.Equal(ConnectType.Remote, ApiClientInfo.ConnectType);
            }
            finally
            {
                ApiClientInfo.ConnectType = originalType;
                ApiClientInfo.Endpoint = originalEndpoint;
                ApiClientInfo.SupportedConnectTypes = originalSupportedTypes;
                s_sysConnField.SetValue(null, originalSysConn);
            }
        }

        [Fact]
        [DisplayName("Initialize(IUIViewService) 有效遠端 URL 端點應在 SyncExecutor.Run 拋例外前執行 SetConnectType（覆蓋 line 197–198）")]
        public void Initialize_ValidRemoteUrl_SetsConnectTypeBeforeThrow()
        {
            var originalStorage = ClientInfo.EndpointStorage;
            var originalType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            var originalSysConn = s_sysConnField.GetValue(null);
            var originalViewService = ClientInfo.UIViewService;
            var originalArgs = ClientInfo.Arguments;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                ClientInfo.EndpointStorage = new FakeEndpointStorage("http://localhost:19999");
                // InitializeConnect: GetEndpoint() → URL → ValidateRemote 通過
                // → SetConnectType (line 197) → SyncExecutor.Run (line 198, 拋例外) → catch 回傳 false
                var result = ClientInfo.Initialize(new FakeUIViewService(false), SupportedConnectTypes.Both);
                Assert.False(result);
                Assert.Equal(ConnectType.Remote, ApiClientInfo.ConnectType);
            }
            finally
            {
                ClientInfo.EndpointStorage = originalStorage;
                ApiClientInfo.ConnectType = originalType;
                ApiClientInfo.Endpoint = originalEndpoint;
                ApiClientInfo.SupportedConnectTypes = originalSupportedTypes;
                s_sysConnField.SetValue(null, originalSysConn);
                s_uiViewServiceProp.GetSetMethod(nonPublic: true)?.Invoke(null, new object?[] { originalViewService });
                s_argumentsProp.GetSetMethod(nonPublic: true)?.Invoke(null, new object?[] { originalArgs });
            }
        }
    }
}
