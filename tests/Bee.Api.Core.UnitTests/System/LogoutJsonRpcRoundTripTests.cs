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
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip：將 <c>System.Logout</c>
    /// 透過 executor 派發到 <see cref="Bee.Business.System.SystemBusinessObject.Logout"/>，
    /// 驗證 SessionInfo 從快取消失且回傳成功。
    /// </summary>
    public class LogoutJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public LogoutJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        private JsonRpcExecutor BuildExecutor(Guid accessToken)
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

        private static JsonRpcRequest BuildRequest()
            => new()
            {
                Method = $"{SysProgIds.System}.{SystemActions.Logout}",
                Params = new JsonRpcParams { Value = new LogoutRequest() },
                Id = Guid.NewGuid().ToString(),
            };

        [Fact]
        [DisplayName("System.Logout 應移除 SessionInfo 並回傳成功")]
        public void Logout_ValidSession_RemovesSessionInfo()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);

            var response = BuildExecutor(accessToken).Execute(BuildRequest());

            Assert.Null(response.Error);
            Assert.IsType<LogoutResponse>(response.Result!.Value);
            Assert.Null(sessionService.Get(accessToken));
        }

        [Fact]
        [DisplayName("System.Logout 對已進公司的 session 應先清 CompanyId 再移除")]
        public void Logout_AfterEnteredCompany_ClearsThenRemoves()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var session = sessionService.Get(accessToken)!;
            session.CompanyId = "C001";
            sessionService.Set(session);

            var response = BuildExecutor(accessToken).Execute(BuildRequest());

            Assert.Null(response.Error);
            Assert.Null(sessionService.Get(accessToken));
        }
    }
}
