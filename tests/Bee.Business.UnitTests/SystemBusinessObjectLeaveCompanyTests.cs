using System.ComponentModel;
using Bee.Business.System;
using Bee.Definition.Identity;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject.LeaveCompany"/> 行為測試。
    /// </summary>
    public class SystemBusinessObjectLeaveCompanyTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectLeaveCompanyTests(SharedDbFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("LeaveCompany 對已進公司的 session 應清空 CompanyId")]
        public void LeaveCompany_WhenEntered_ClearsCompanyId()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var session = sessionService.Get(accessToken)!;
            session.CompanyId = "C001";
            sessionService.Set(session);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                var result = bo.LeaveCompany(new LeaveCompanyArgs());

                Assert.NotNull(result);
                Assert.Null(sessionService.Get(accessToken)!.CompanyId);
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("LeaveCompany 對未進公司的 session 應 idempotent 回傳成功")]
        public void LeaveCompany_WhenNotEntered_Idempotent()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                var result = bo.LeaveCompany(new LeaveCompanyArgs());

                Assert.NotNull(result);
                Assert.Null(sessionService.Get(accessToken)!.CompanyId);
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("LeaveCompany 對 null args 應拋 ArgumentNullException")]
        public void LeaveCompany_NullArgs_ThrowsArgumentNullException()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();

            try
            {
                Assert.Throws<ArgumentNullException>(() => bo.LeaveCompany(null!));
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("LeaveCompany 對無效 session 應拋 UnauthorizedAccessException")]
        public void LeaveCompany_NoSession_ThrowsUnauthorizedAccessException()
        {
            // 直接用一個未植入的 AccessToken
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid());

            Assert.Throws<UnauthorizedAccessException>(() => bo.LeaveCompany(new LeaveCompanyArgs()));
        }
    }
}
