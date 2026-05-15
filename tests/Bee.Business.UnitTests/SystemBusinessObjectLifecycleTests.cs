using System.ComponentModel;
using Bee.Business.System;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition.Identity;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// SystemBO session lifecycle 全流程整合測試。串接 Login → EnterCompany(A) →
    /// EnterCompany(B) → LeaveCompany → EnterCompany(A) → Logout，驗證跨四個方法的
    /// session state transition 一致性，並涵蓋 plan-system-bo-session-lifecycle.md
    /// 列出的合法 / 非法路徑。
    /// </summary>
    public class SystemBusinessObjectLifecycleTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectLifecycleTests(SharedDbFixture fx) { _fx = fx; }

        private static string UniqueCompanyId() => "C_" + Guid.NewGuid().ToString("N")[..12];

        [Fact]
        [DisplayName("Login → EnterCompany(A) → EnterCompany(B) → LeaveCompany → EnterCompany(A) → Logout 整條 session lifecycle 應一致")]
        public void FullLifecycle_LoginThroughLogout_TransitionsCorrectly()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var companyService = _fx.GetRequiredService<ICompanyInfoService>();
            var companyA = UniqueCompanyId();
            var companyB = UniqueCompanyId();
            companyService.Set(new CompanyInfo { CompanyId = companyA, CompanyName = "Acme", CompanyDatabaseId = "biz_a", LogDatabaseId = "log_a" });
            companyService.Set(new CompanyInfo { CompanyId = companyB, CompanyName = "Bee", CompanyDatabaseId = "biz_b", LogDatabaseId = "log_b" });

            try
            {
                // 1. Login — 用 TestableSystemBusinessObject 繞過預設的 AuthenticateUser=false
                var loginBo = new TestableSystemBusinessObject(
                    TestBeeContext.Create(_fx),
                    Guid.Empty,
                    _ => (true, "Integration User"));
                var loginResult = loginBo.Login(new LoginArgs { UserId = "lifecycle_user", Password = "pwd" });
                Assert.NotEqual(Guid.Empty, loginResult.AccessToken);
                var accessToken = loginResult.AccessToken;
                Assert.Null(sessionService.Get(accessToken)!.CompanyId);

                // 後續方法用一般 SystemBusinessObject + Login 取得的 AccessToken
                var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

                // 2. EnterCompany(A) — 首次進公司
                var enterA = bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyA });
                Assert.Equal(companyA, enterA.Company.CompanyId);
                Assert.Equal(companyA, sessionService.Get(accessToken)!.CompanyId);

                // 3. EnterCompany(B) — 切換（直接覆寫）
                var enterB = bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyB });
                Assert.Equal(companyB, enterB.Company.CompanyId);
                Assert.Equal(companyB, sessionService.Get(accessToken)!.CompanyId);

                // 4. LeaveCompany — 清回未進公司狀態
                bo.LeaveCompany(new LeaveCompanyArgs());
                Assert.Null(sessionService.Get(accessToken)!.CompanyId);

                // 5. EnterCompany(A) again — Leave 後重新進，狀態無痕殘留
                var enterAAgain = bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyA });
                Assert.Equal(companyA, enterAAgain.Company.CompanyId);
                Assert.Equal(companyA, sessionService.Get(accessToken)!.CompanyId);

                // 6. Logout — 隱含 LeaveCompany 清理，整個 session 從快取消失
                bo.Logout(new LogoutArgs());
                Assert.Null(sessionService.Get(accessToken));
            }
            finally
            {
                companyService.Remove(companyA);
                companyService.Remove(companyB);
            }
        }

        [Fact]
        [DisplayName("Logout 後再呼叫 EnterCompany 應拋 UnauthorizedAccessException")]
        public void AfterLogout_EnterCompany_ThrowsUnauthorized()
        {
            var companyService = _fx.GetRequiredService<ICompanyInfoService>();
            var companyId = UniqueCompanyId();
            companyService.Set(new CompanyInfo { CompanyId = companyId, CompanyName = "Acme" });

            try
            {
                var loginBo = new TestableSystemBusinessObject(
                    TestBeeContext.Create(_fx), Guid.Empty,
                    _ => (true, "User"));
                var accessToken = loginBo.Login(new LoginArgs { UserId = "u", Password = "p" }).AccessToken;
                var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);
                bo.Logout(new LogoutArgs());

                Assert.Throws<UnauthorizedAccessException>(
                    () => bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyId }));
            }
            finally
            {
                companyService.Remove(companyId);
            }
        }

        [Fact]
        [DisplayName("Login 後直接 Logout（未進公司）應 idempotent 通過")]
        public void Login_DirectLogout_WithoutEnteringCompany_Succeeds()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var loginBo = new TestableSystemBusinessObject(
                TestBeeContext.Create(_fx), Guid.Empty,
                _ => (true, "User"));
            var accessToken = loginBo.Login(new LoginArgs { UserId = "u", Password = "p" }).AccessToken;
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            bo.Logout(new LogoutArgs());

            Assert.Null(sessionService.Get(accessToken));
        }

        [Fact]
        [DisplayName("Login 後 LeaveCompany（未進公司）應 idempotent；SessionInfo.CompanyId 維持 null")]
        public void Login_LeaveCompanyWithoutEntering_Idempotent()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var loginBo = new TestableSystemBusinessObject(
                TestBeeContext.Create(_fx), Guid.Empty,
                _ => (true, "User"));
            var accessToken = loginBo.Login(new LoginArgs { UserId = "u", Password = "p" }).AccessToken;
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                bo.LeaveCompany(new LeaveCompanyArgs());
                Assert.Null(sessionService.Get(accessToken)!.CompanyId);
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }
    }
}
