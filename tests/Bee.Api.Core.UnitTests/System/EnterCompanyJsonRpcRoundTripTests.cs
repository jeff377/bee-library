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
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip：將 <c>System.EnterCompany</c>
    /// 透過 executor 派發到 <see cref="Bee.Business.System.SystemBusinessObject.EnterCompany"/>，
    /// 驗證：
    /// <list type="bullet">
    /// <item>action 路由（progId.action 反射查表）正確找到方法</item>
    /// <item>ApiInputConverter（EnterCompanyRequest → EnterCompanyArgs）保留 CompanyId</item>
    /// <item>ApiOutputConverter（EnterCompanyResult → EnterCompanyResponse）命名慣例反射有作用，CompanyInfo deep-copy 正確</item>
    /// <item>BO 寫入 SessionInfo.CompanyId 後可被 ISessionInfoService 取回</item>
    /// </list>
    /// </summary>
    public class EnterCompanyJsonRpcRoundTripTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public EnterCompanyJsonRpcRoundTripTests(SharedDbFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("System.EnterCompany 經 JsonRpcExecutor 應派發成功並寫入 SessionInfo.CompanyId")]
        public void EnterCompany_ThroughJsonRpc_DispatchesAndBindsCompany()
        {
            // Arrange: 利用 SharedDatabaseState 已 seed 的 user '001' + company 'C001' 對照，
            // 走完整的 cache miss → DB fallback + HasAccess JOIN 路徑。
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: "001");

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
                Method = $"{SysProgIds.System}.{SystemActions.EnterCompany}",
                Params = new JsonRpcParams
                {
                    Value = new EnterCompanyRequest { CompanyId = "C001" },
                },
                Id = Guid.NewGuid().ToString(),
            };

            // Act
            var response = executor.Execute(request);

            // Assert: response 成功並帶 CompanyInfo（cache miss 後從 DB 載入 seed company 'C001'）
            Assert.Null(response.Error);
            var result = Assert.IsType<EnterCompanyResponse>(response.Result!.Value);
            Assert.NotNull(result.Company);
            Assert.Equal("C001", result.Company.CompanyId);
            Assert.Equal("測試公司", result.Company.CompanyName);

            // SessionInfo.CompanyId 已寫入
            var session = _fx.GetRequiredService<ISessionInfoService>().Get(accessToken);
            Assert.NotNull(session);
            Assert.Equal("C001", session.CompanyId);
        }

        [Fact]
        [DisplayName("System.EnterCompany 對不存在的 CompanyId 應回 RpcError 且 SessionInfo.CompanyId 不變")]
        public void EnterCompany_UnknownCompany_ReturnsRpcError()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, userId: "001");

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
                Method = $"{SysProgIds.System}.{SystemActions.EnterCompany}",
                Params = new JsonRpcParams
                {
                    Value = new EnterCompanyRequest { CompanyId = "NO_SUCH_COMPANY" },
                },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.NotNull(response.Error);
            Assert.Contains("Company access denied", response.Error!.Message);

            var session = _fx.GetRequiredService<ISessionInfoService>().Get(accessToken);
            Assert.NotNull(session);
            Assert.Null(session.CompanyId);
        }
    }
}
