using System.ComponentModel;
using Bee.Business.System;
using Bee.Definition.Identity;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject.EnterCompany"/> 行為測試。每個測試用獨立的
    /// AccessToken 與 CompanyId，避免共享 fixture 內的 session / company cache 互相干擾。
    /// </summary>
    public class SystemBusinessObjectEnterCompanyTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectEnterCompanyTests(SharedDbFixture fx) { _fx = fx; }

        private static string UniqueCompanyId() => "C_" + Guid.NewGuid().ToString("N")[..12];

        [Fact]
        [DisplayName("EnterCompany 對已存在的 CompanyId 應回傳 CompanyInfo 並設定 SessionInfo.CompanyId")]
        public void EnterCompany_ValidCompany_BindsAndReturns()
        {
            var companyService = _fx.GetRequiredService<ICompanyInfoService>();
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var companyId = UniqueCompanyId();
            companyService.Set(new CompanyInfo
            {
                CompanyId = companyId,
                CompanyName = "Acme",
                CompanyDatabaseId = "biz_01"
            });
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                var result = bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyId });

                Assert.NotNull(result);
                Assert.Equal(companyId, result.Company.CompanyId);
                Assert.Equal("Acme", result.Company.CompanyName);
                Assert.Equal("biz_01", result.Company.CompanyDatabaseId);

                var session = sessionService.Get(accessToken);
                Assert.NotNull(session);
                Assert.Equal(companyId, session.CompanyId);
            }
            finally
            {
                companyService.Remove(companyId);
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 對不存在的 CompanyId 應拋 InvalidOperationException 且 SessionInfo.CompanyId 不變")]
        public void EnterCompany_UnknownCompany_ThrowsAndLeavesSessionUnchanged()
        {
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                var ex = Assert.Throws<InvalidOperationException>(
                    () => bo.EnterCompany(new EnterCompanyArgs { CompanyId = UniqueCompanyId() }));
                Assert.Contains("Company access denied", ex.Message);

                var session = sessionService.Get(accessToken);
                Assert.NotNull(session);
                Assert.Null(session.CompanyId);
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 切換到不同 CompanyId 應覆寫 SessionInfo.CompanyId")]
        public void EnterCompany_SwitchToAnotherCompany_Overwrites()
        {
            var companyService = _fx.GetRequiredService<ICompanyInfoService>();
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var companyA = UniqueCompanyId();
            var companyB = UniqueCompanyId();
            companyService.Set(new CompanyInfo { CompanyId = companyA, CompanyName = "A" });
            companyService.Set(new CompanyInfo { CompanyId = companyB, CompanyName = "B" });
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyA });
                Assert.Equal(companyA, sessionService.Get(accessToken)!.CompanyId);

                bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyB });
                Assert.Equal(companyB, sessionService.Get(accessToken)!.CompanyId);
            }
            finally
            {
                companyService.Remove(companyA);
                companyService.Remove(companyB);
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 對同一 CompanyId 重複呼叫應 idempotent")]
        public void EnterCompany_SameCompany_Idempotent()
        {
            var companyService = _fx.GetRequiredService<ICompanyInfoService>();
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var companyId = UniqueCompanyId();
            companyService.Set(new CompanyInfo { CompanyId = companyId, CompanyName = "Acme" });
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);

            try
            {
                bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyId });
                bo.EnterCompany(new EnterCompanyArgs { CompanyId = companyId });

                Assert.Equal(companyId, sessionService.Get(accessToken)!.CompanyId);
            }
            finally
            {
                companyService.Remove(companyId);
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 對空 CompanyId 應拋 ArgumentException")]
        public void EnterCompany_EmptyCompanyId_ThrowsArgumentException()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();

            try
            {
                Assert.Throws<ArgumentException>(
                    () => bo.EnterCompany(new EnterCompanyArgs { CompanyId = string.Empty }));
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }

        [Fact]
        [DisplayName("EnterCompany 對 null args 應拋 ArgumentNullException")]
        public void EnterCompany_NullArgs_ThrowsArgumentNullException()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();

            try
            {
                Assert.Throws<ArgumentNullException>(() => bo.EnterCompany(null!));
            }
            finally
            {
                sessionService.Remove(accessToken);
            }
        }
    }
}
