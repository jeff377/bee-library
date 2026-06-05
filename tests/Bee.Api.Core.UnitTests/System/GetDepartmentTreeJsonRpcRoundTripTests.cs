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
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip：將 <c>System.GetDepartmentTree</c>
    /// 透過 executor 派發到 <see cref="Bee.Business.System.SystemBusinessObject.GetDepartmentTree"/>，
    /// 驗證 action 路由、ApiInputConverter（Request→Args）、ApiOutputConverter（Result→Response）。
    /// 未 EnterCompany 時回 null tree（不碰 DB，聚焦 dispatch 路徑）。
    /// </summary>
    public class GetDepartmentTreeJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;
        public GetDepartmentTreeJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("System.GetDepartmentTree 經 JsonRpcExecutor 派發成功；未進公司回 null tree")]
        public void GetDepartmentTree_ThroughJsonRpc_NoCompany_ReturnsNullTree()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);

            var boFactory = new BusinessObjectFactory(
                _fx.Provider,
                _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<ISessionInfoService>(),
                _fx.GetRequiredService<ILanguageService>(),
                _fx.GetRequiredService<IFormBoTypeResolver>());

            var executor = new JsonRpcExecutor(
                boFactory,
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = accessToken,
                IsLocalCall = true,
            };

            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.{SystemActions.GetDepartmentTree}",
                Params = new JsonRpcParams { Value = new GetDepartmentTreeRequest() },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<GetDepartmentTreeResponse>(response.Result!.Value);
            Assert.Null(result.Tree); // 未 EnterCompany → CompanyId 空 → 不查 service、tree 為 null
        }
    }
}
