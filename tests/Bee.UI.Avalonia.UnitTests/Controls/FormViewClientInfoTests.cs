using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.Providers;
using Bee.Tests.Shared;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Core;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// 驗證 <see cref="FormView"/> 的預設 Resolve 路徑委派到 <see cref="ClientInfo"/>，
    /// 涵蓋 Local 與 Remote 兩種連線模式。
    /// </summary>
    /// <remarks>
    /// <see cref="ClientInfo"/> / <see cref="ApiClientInfo"/> 持有 process-wide 靜態狀態；
    /// 每個測試以 <see cref="ClientInfoTestScope"/> 保護，並透過 <c>[Collection("ClientInfo")]</c>
    /// 與其他修改同一靜態狀態的 test class 串行執行。
    /// </remarks>
    [Collection("ClientInfo")]
    public class FormViewClientInfoTests
    {
        private const string TestProgId = "Employee";
        private static readonly object[] s_testProgIdArgs = [TestProgId];

        [Theory]
        [InlineData(ConnectType.Local)]
        [InlineData(ConnectType.Remote)]
        [DisplayName("預設 Resolve 路徑會委派到 ClientInfo，Local/Remote 兩種模式都可正確建立連線物件")]
        public void Default_Resolve_DelegatesToClientInfo(ConnectType connectType)
        {
            using var scope = new ClientInfoTestScope();
            ApiClientInfo.ConnectType = connectType;
            ApiClientInfo.Endpoint = connectType == ConnectType.Remote
                ? "http://localhost/jsonrpc/api"
                : string.Empty;

            // Clear cached connector so it is rebuilt with the current ConnectType.
            var systemConnectorField = typeof(ClientInfo).GetField(
                "_systemConnector", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(systemConnectorField);
            systemConnectorField!.SetValue(null, null);

            var view = new FormView();
            var resolveSystem = typeof(FormView).GetMethod(
                "ResolveSystemConnector", BindingFlags.NonPublic | BindingFlags.Instance);
            var resolveForm = typeof(FormView).GetMethod(
                "ResolveFormConnector", BindingFlags.NonPublic | BindingFlags.Instance);
            var resolveToken = typeof(FormView).GetMethod(
                "ResolveAccessToken", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(resolveSystem);
            Assert.NotNull(resolveForm);
            Assert.NotNull(resolveToken);

            var system = (SystemApiConnector?)resolveSystem!.Invoke(view, Array.Empty<object>());
            Assert.NotNull(system);
            var form = (FormApiConnector)resolveForm!.Invoke(view, s_testProgIdArgs)!;
            Assert.Equal(TestProgId, form.ProgId);
            var token = (Guid)resolveToken!.Invoke(view, Array.Empty<object>())!;
            Assert.Equal(ClientInfo.AccessToken, token);

            var expectedProviderType = connectType == ConnectType.Local
                ? typeof(LocalApiProvider)
                : typeof(RemoteApiProvider);
            Assert.IsType(expectedProviderType, system!.Provider);
            Assert.IsType(expectedProviderType, form.Provider);
        }
    }
}
