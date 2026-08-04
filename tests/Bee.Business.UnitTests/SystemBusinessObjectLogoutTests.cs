using System.ComponentModel;
using Bee.Business.System;
using Bee.Definition.Identity;
using Bee.Tests.Shared;

using Bee.Definition;
namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject.Logout"/> 行為測試。
    /// </summary>
    public class SystemBusinessObjectLogoutTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectLogoutTests(SharedDbFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("Logout 對有效 session 應移除 SessionInfo")]
        public void Logout_ValidSession_RemovesSessionInfo()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken, SysProgIds.System);

            var result = bo.Logout(new LogoutArgs());

            Assert.NotNull(result);
            Assert.Null(sessionService.Get(accessToken));
        }

        [Fact]
        [DisplayName("Logout 對已進公司的 session 應清 CompanyId 並移除 session")]
        public void Logout_AfterEnteredCompany_ClearsCompanyIdAndRemoves()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var session = sessionService.Get(accessToken)!;
            session.CompanyId = "C001";
            sessionService.Set(session);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken, SysProgIds.System);

            bo.Logout(new LogoutArgs());

            Assert.Null(sessionService.Get(accessToken));
        }

        [Fact]
        [DisplayName("Logout 對不存在的 session 應 idempotent 回傳成功")]
        public void Logout_UnknownSession_Idempotent()
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid(), SysProgIds.System);

            var result = bo.Logout(new LogoutArgs());

            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("Logout 對 null args 應拋 ArgumentNullException")]
        public void Logout_NullArgs_ThrowsArgumentNullException()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken, SysProgIds.System);
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();

            try
            {
                Assert.Throws<ArgumentNullException>(() => bo.Logout(null!));
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }
    }
}
