using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.Providers;
using Bee.Api.Core;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.System;
using Bee.Web.Blazor.Wasm.Components;
using Bee.Web.Blazor.Wasm.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// 補強 <see cref="BeeLoginPanel.OnSubmitAsync"/> 成功路徑的覆蓋率。
    /// 使用假 Factory + 假 IJsonRpcProvider，讓 LoginAsync 在不需要真實 API 服務的情況下
    /// 回傳可控制的 <see cref="LoginResponse"/>，覆蓋以下四條路徑：
    /// 1. AccessToken 為空 → 設定錯誤訊息並提前返回
    /// 2. AccessToken 有效 → 清除密碼欄位
    /// 3. AccessToken 有效且無 OnLoggedIn 委派 → 正常完成
    /// 4. AccessToken 有效且有 OnLoggedIn 委派 → 呼叫 callback
    /// </summary>
    public class BeeLoginPanelSuccessPathTests
    {
        private static readonly FieldInfo s_errorField =
            typeof(BeeLoginPanel).GetField("_error", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo s_passwordField =
            typeof(BeeLoginPanel).GetField("_password", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly PropertyInfo s_factoryProp =
            typeof(BeeLoginPanel).GetProperty("Factory", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private sealed class FakeLoginProvider : IJsonRpcProvider
        {
            private readonly LoginResponse _response;

            public FakeLoginProvider(LoginResponse response) => _response = response;

            public Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
            {
                var type = typeof(LoginResponse);
                var bytes = ApiServiceOptions.PayloadTransformer.Encode(_response, type);
                var result = new JsonRpcResult
                {
                    TypeName = $"{type.FullName}, {type.Assembly.GetName().Name}",
                    Value = bytes
                };
                return Task.FromResult(new JsonRpcResponse { Result = result });
            }
        }

        private sealed class FakeConnectorFactory : BeeApiConnectorFactory
        {
            private readonly IJsonRpcProvider _provider;

            public FakeConnectorFactory(IJsonRpcProvider provider)
                : base(new BeeBlazorOptions().UseRemoteProvider("http://unused.local"))
                => _provider = provider;

            public override SystemApiConnector CreateSystemConnector(Guid accessToken)
            {
                var connector = new SystemApiConnector(accessToken);
                connector.GetType()
                    .GetProperty("Provider", BindingFlags.Public | BindingFlags.Instance)!
                    .GetSetMethod(nonPublic: true)!
                    .Invoke(connector, new object[] { _provider });
                return connector;
            }
        }

        private sealed class SyncEventHandler : IHandleEvent
        {
            public Task HandleEventAsync(EventCallbackWorkItem callback, object? arg)
                => callback.InvokeAsync(arg);
        }

        private static async Task InvokeOnSubmitAsync(BeeLoginPanel panel)
        {
            var method = typeof(BeeLoginPanel).GetMethod(
                "OnSubmitAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)method.Invoke(panel, null)!;
        }

        private static BeeLoginPanel CreatePanelWithFakeFactory(LoginResponse response)
        {
            var panel = new BeeLoginPanel();
            var factory = new FakeConnectorFactory(new FakeLoginProvider(response));
            s_factoryProp.SetValue(panel, factory);
            return panel;
        }

        [Fact]
        [DisplayName("OnSubmitAsync LoginAsync 回傳空 AccessToken 時應設定登入失敗的錯誤訊息")]
        public async Task OnSubmitAsync_EmptyAccessToken_SetsLoginFailedError()
        {
            var panel = CreatePanelWithFakeFactory(new LoginResponse { AccessToken = Guid.Empty });

            await InvokeOnSubmitAsync(panel);

            Assert.Equal(
                "Login failed: the server returned an empty access token.",
                (string?)s_errorField.GetValue(panel));
        }

        [Fact]
        [DisplayName("OnSubmitAsync 登入成功後應清除密碼欄位為空字串")]
        public async Task OnSubmitAsync_SuccessfulLogin_ClearsPasswordField()
        {
            var panel = CreatePanelWithFakeFactory(new LoginResponse { AccessToken = Guid.NewGuid() });
            s_passwordField.SetValue(panel, "secret");

            await InvokeOnSubmitAsync(panel);

            Assert.Equal(string.Empty, (string?)s_passwordField.GetValue(panel));
        }

        [Fact]
        [DisplayName("OnSubmitAsync 登入成功且無 OnLoggedIn 委派時不拋例外，且不設定錯誤訊息")]
        public async Task OnSubmitAsync_SuccessfulLoginNoDelegate_NoErrorSet()
        {
            var panel = CreatePanelWithFakeFactory(new LoginResponse { AccessToken = Guid.NewGuid() });

            await InvokeOnSubmitAsync(panel);

            Assert.Null((string?)s_errorField.GetValue(panel));
        }

        [Fact]
        [DisplayName("OnSubmitAsync 登入成功且有 OnLoggedIn 委派時應呼叫 callback 並傳入 LoginResponse")]
        public async Task OnSubmitAsync_SuccessfulLoginWithDelegate_InvokesCallback()
        {
            var expectedToken = Guid.NewGuid();
            var panel = CreatePanelWithFakeFactory(new LoginResponse { AccessToken = expectedToken });

            LoginResponse? captured = null;
            typeof(BeeLoginPanel)
                .GetProperty("OnLoggedIn", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(panel, EventCallback.Factory.Create<LoginResponse>(
                    new SyncEventHandler(),
                    (LoginResponse r) => { captured = r; }));

            await InvokeOnSubmitAsync(panel);

            Assert.NotNull(captured);
            Assert.Equal(expectedToken, captured!.AccessToken);
        }
    }
}
