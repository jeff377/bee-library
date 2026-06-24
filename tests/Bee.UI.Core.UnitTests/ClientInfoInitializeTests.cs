using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo.InitializeAsync(IUIViewService,SupportedConnectTypes)"/> 的覆蓋率。
    /// 此方法在端點無效時會呼叫 <see cref="IUIViewService.ShowApiConnectAsync"/>；
    /// 以輕量 fake 取代真實 UI 服務，無需啟動後端即可覆蓋 try-catch 路徑。
    /// 與其他修改靜態狀態的測試同屬 <c>ClientInfoState</c> collection，確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoInitializeTests
    {
        private sealed class FakeUIViewService : IUIViewService
        {
            private readonly bool _result;
            public FakeUIViewService(bool result) { _result = result; }
            public Task<bool> ShowApiConnectAsync() => Task.FromResult(_result);
        }

        private static readonly PropertyInfo s_uiViewServiceProp =
            typeof(ClientInfo).GetProperty("UIViewService",
                BindingFlags.Public | BindingFlags.Static)!;

        private static readonly PropertyInfo s_argumentsProp =
            typeof(ClientInfo).GetProperty("Arguments",
                BindingFlags.Public | BindingFlags.Static)!;

        private static void RestoreState(
            SupportedConnectTypes originalSupportedTypes,
            IUIViewService? originalViewService,
            Dictionary<string, string>? originalArgs)
        {
            ApiClientInfo.SupportedConnectTypes = originalSupportedTypes;
            s_uiViewServiceProp.GetSetMethod(nonPublic: true)?.Invoke(null, new object?[] { originalViewService });
            s_argumentsProp.GetSetMethod(nonPublic: true)?.Invoke(null, new object?[] { originalArgs });
        }

        [Fact]
        [DisplayName("InitializeAsync(IUIViewService) ShowApiConnectAsync 回傳 false 時應回傳 false")]
        public async Task InitializeAsync_ShowApiConnectReturnsFalse_ReturnsFalse()
        {
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            var originalViewService = ClientInfo.UIViewService;
            var originalArgs = ClientInfo.Arguments;
            try
            {
                var result = await ClientInfo.InitializeAsync(new FakeUIViewService(false), SupportedConnectTypes.Both);
                Assert.False(result);
            }
            finally
            {
                RestoreState(originalSupportedTypes, originalViewService, originalArgs);
            }
        }

        [Fact]
        [DisplayName("InitializeAsync(IUIViewService) ShowApiConnectAsync 回傳 true 時應回傳 true")]
        public async Task InitializeAsync_ShowApiConnectReturnsTrue_ReturnsTrue()
        {
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            var originalViewService = ClientInfo.UIViewService;
            var originalArgs = ClientInfo.Arguments;
            try
            {
                var result = await ClientInfo.InitializeAsync(new FakeUIViewService(true), SupportedConnectTypes.Both);
                Assert.True(result);
            }
            finally
            {
                RestoreState(originalSupportedTypes, originalViewService, originalArgs);
            }
        }

        [Fact]
        [DisplayName("InitializeAsync(IUIViewService) 呼叫後 UIViewService 應設為傳入的 service 實例")]
        public async Task InitializeAsync_SetsUIViewServiceToPassedInstance()
        {
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            var originalViewService = ClientInfo.UIViewService;
            var originalArgs = ClientInfo.Arguments;
            try
            {
                var service = new FakeUIViewService(true);
                await ClientInfo.InitializeAsync(service, SupportedConnectTypes.Both);
                Assert.Same(service, ClientInfo.UIViewService);
            }
            finally
            {
                RestoreState(originalSupportedTypes, originalViewService, originalArgs);
            }
        }

        [Fact]
        [DisplayName("InitializeAsync(IUIViewService) 呼叫後 Arguments 應為非 null 字典")]
        public async Task InitializeAsync_SetsArgumentsToNonNull()
        {
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            var originalViewService = ClientInfo.UIViewService;
            var originalArgs = ClientInfo.Arguments;
            try
            {
                await ClientInfo.InitializeAsync(new FakeUIViewService(true), SupportedConnectTypes.Both);
                Assert.NotNull(ClientInfo.Arguments);
            }
            finally
            {
                RestoreState(originalSupportedTypes, originalViewService, originalArgs);
            }
        }
    }

    /// <summary>
    /// 補強 <see cref="ClientInfo.InitializeAsync(string)"/> 的覆蓋率。
    /// 空字串端點會在 <see cref="ApiConnectValidator.ValidateAsync"/> 前即失敗，
    /// 不修改任何靜態狀態，無需加入序列化 collection。
    /// </summary>
    public class ClientInfoStringEndpointTests
    {
        [Fact]
        [DisplayName("InitializeAsync(string) 空字串端點應拋 ArgumentException")]
        public async Task InitializeAsync_EmptyStringEndpoint_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => ClientInfo.InitializeAsync(string.Empty));
        }
    }
}
