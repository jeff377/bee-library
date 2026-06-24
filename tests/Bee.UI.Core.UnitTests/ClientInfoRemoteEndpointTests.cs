using System.ComponentModel;
using Bee.Api.Client;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 驗證 <see cref="ClientInfo"/> 在遠端端點無法連線時的防護行為。
    /// <para>
    /// <c>ApiConnectValidator.ValidateRemoteAsync</c> 會先以 HTTP HEAD 探測連線性
    /// （<c>HttpUtilities.IsEndpointReachableAsync</c>），再呼叫 <c>PingAsync</c>；
    /// 任一步驟失敗均在 <c>SetConnectType</c> 被呼叫之前拋出例外。
    /// 本類別僅驗證此拋出行為本身，以及 <c>InitializeConnectAsync</c> 對該例外的吞除策略。
    /// </para>
    /// 因修改靜態狀態，與其他 ClientInfoState 測試同屬 collection，確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoRemoteEndpointTests
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
            private readonly bool _result;
            public FakeUIViewService(bool result) => _result = result;
            public Task<bool> ShowApiConnectAsync() => Task.FromResult(_result);
        }

        [Fact]
        [DisplayName("InitializeAsync(IUIViewService) 無法連線的遠端 URL 端點應回傳 false")]
        public async Task InitializeAsync_UnreachableRemoteUrl_ReturnsFalse()
        {
            var originalStorage = ClientInfo.EndpointStorage;
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                ClientInfo.EndpointStorage = new FakeEndpointStorage("http://localhost:19999");
                var result = await ClientInfo.InitializeAsync(new FakeUIViewService(false), SupportedConnectTypes.Both);
                Assert.False(result);
            }
            finally
            {
                ClientInfo.EndpointStorage = originalStorage;
                ApiClientInfo.SupportedConnectTypes = originalSupportedTypes;
            }
        }

        [Fact]
        [DisplayName("SetEndpointAsync 無法連線的遠端 URL 應拋出 InvalidOperationException")]
        public async Task SetEndpointAsync_UnreachableRemoteUrl_ThrowsInvalidOperationException()
        {
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                var ex = await Record.ExceptionAsync(() => ClientInfo.SetEndpointAsync("http://localhost:19999"));
                Assert.IsType<InvalidOperationException>(ex);
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = originalSupportedTypes;
            }
        }

        [Fact]
        [DisplayName("InitializeAsync(string) 無法連線的遠端 URL 應拋出 InvalidOperationException")]
        public async Task InitializeAsync_UnreachableRemoteUrl_ThrowsInvalidOperationException()
        {
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                var ex = await Record.ExceptionAsync(() => ClientInfo.InitializeAsync("http://localhost:19999"));
                Assert.IsType<InvalidOperationException>(ex);
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = originalSupportedTypes;
            }
        }
    }
}
