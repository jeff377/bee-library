using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.System;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests.System
{
    /// <summary>
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip：將
    /// <c>System.GetFormLayout</c> 透過 executor 派發到
    /// <see cref="Bee.Business.System.SystemBusinessObject.GetFormLayout"/>，驗證：
    /// <list type="bullet">
    /// <item>action 路由（progId.action 反射查表）正確找到方法</item>
    /// <item>ApiInputConverter（GetFormLayoutRequest → GetFormLayoutArgs）保留 ProgId / LayoutId</item>
    /// <item>ApiOutputConverter（GetFormLayoutResult → GetFormLayoutResponse）命名慣例反射有作用，
    ///   FormLayout 物件 deep-copy 正確</item>
    /// <item>LayoutId 空字串時 server 預設取 "default"</item>
    /// </list>
    /// </summary>
    public class GetFormLayoutJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public GetFormLayoutJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        private JsonRpcExecutor NewExecutor(Guid accessToken)
        {
            var boFactory = new BusinessObjectFactory(
                _fx.Provider,
                _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<ISessionInfoService>(),
                _fx.GetRequiredService<ILanguageService>(),
                _fx.GetRequiredService<IFormBoTypeResolver>());

            return new JsonRpcExecutor(
                boFactory,
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = accessToken,
                IsLocalCall = true,
            };
        }

        [Fact]
        [DisplayName("System.GetFormLayout 經 JsonRpcExecutor 應派發成功並回傳預設 layout")]
        public void GetFormLayout_ThroughJsonRpc_DispatchesAndReturnsLayout()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var executor = NewExecutor(accessToken);

            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.{SystemActions.GetFormLayout}",
                Params = new JsonRpcParams
                {
                    Value = new GetFormLayoutRequest { ProgId = "Employee", LayoutId = "" },
                },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<GetFormLayoutResponse>(response.Result!.Value);
            Assert.NotNull(result.Layout);
            Assert.Equal("Employee", result.Layout!.ProgId);
            Assert.Equal("default", result.Layout.LayoutId);
            Assert.NotNull(result.Layout.Sections);
            Assert.True(result.Layout.Sections!.Count > 0, "Layout 應至少有一個 section");
        }

        [Fact]
        [DisplayName("System.GetFormLayout 對空 ProgId 應回 RpcError")]
        public void GetFormLayout_EmptyProgId_ReturnsRpcError()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var executor = NewExecutor(accessToken);

            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.{SystemActions.GetFormLayout}",
                Params = new JsonRpcParams
                {
                    Value = new GetFormLayoutRequest { ProgId = "", LayoutId = "default" },
                },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.NotNull(response.Error);
            Assert.Contains("ProgId is required", response.Error!.Message);
        }
    }
}
