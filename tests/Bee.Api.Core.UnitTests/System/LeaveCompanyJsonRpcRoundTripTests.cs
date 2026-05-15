using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.System;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests.System
{
    /// <summary>
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip：將 <c>System.LeaveCompany</c>
    /// 透過 executor 派發到 <see cref="Bee.Business.System.SystemBusinessObject.LeaveCompany"/>，
    /// 驗證 SessionInfo.CompanyId 被清空且回傳成功。
    /// </summary>
    public class LeaveCompanyJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public LeaveCompanyJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        private JsonRpcExecutor BuildExecutor(Guid accessToken)
        {
            var boFactory = new BusinessObjectFactory(
                _fx.Provider,
                _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<ISessionInfoService>(),
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
                Method = $"{SysProgIds.System}.{SystemActions.LeaveCompany}",
                Params = new JsonRpcParams { Value = new LeaveCompanyRequest() },
                Id = Guid.NewGuid().ToString(),
            };

        [Fact]
        [DisplayName("System.LeaveCompany 應清空 SessionInfo.CompanyId 並回傳成功")]
        public void LeaveCompany_AfterEntered_ClearsCompanyId()
        {
            // Arrange：建立 session 並設 CompanyId
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var session = sessionService.Get(accessToken)!;
            session.CompanyId = "C001";
            sessionService.Set(session);

            // Act
            var response = BuildExecutor(accessToken).Execute(BuildRequest());

            // Assert
            Assert.Null(response.Error);
            Assert.IsType<LeaveCompanyResponse>(response.Result!.Value);
            Assert.Null(sessionService.Get(accessToken)!.CompanyId);
        }

        [Fact]
        [DisplayName("System.LeaveCompany 對未進公司狀態應 idempotent 回傳成功")]
        public void LeaveCompany_WhenNotEntered_Idempotent()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            Assert.Null(sessionService.Get(accessToken)!.CompanyId);

            var response = BuildExecutor(accessToken).Execute(BuildRequest());

            Assert.Null(response.Error);
            Assert.IsType<LeaveCompanyResponse>(response.Result!.Value);
            Assert.Null(sessionService.Get(accessToken)!.CompanyId);
        }
    }
}
