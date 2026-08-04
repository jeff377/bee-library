using System.ComponentModel;
using Bee.Business.System;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Bee.Tests.Shared;

using Bee.Definition;
namespace Bee.Business.UnitTests
{
    /// <summary>
    /// 驗證 session 生命週期的四個寫入點確實落到 <c>st_session</c> 種子：
    /// Login 寫入、EnterCompany / 切換公司更新、LeaveCompany 清空、Logout 刪除。
    /// </summary>
    /// <remarks>
    /// 這四點缺一不可：Login 不寫，token 一交付即在部署或多節點下失效；Logout 不刪，
    /// 登出後 token 會由種子重建復活，登出形同虛設。
    /// </remarks>
    public class SystemBusinessObjectSessionSeedTests : IClassFixture<SharedDbFixture>
    {
        private const string SeedCompanyId = "C001";
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectSessionSeedTests(SharedDbFixture fx) { _fx = fx; }

        private ISessionRepository SessionRepository
            => _fx.GetRequiredService<ISystemRepositoryFactory>().CreateSessionRepository();

        private Guid LoginAsSeedUser()
        {
            var bo = new TestableSystemBusinessObject(
                TestBeeContext.Create(_fx), Guid.Empty, _ => (true, "Seed User"));
            return bo.Login(new LoginArgs { UserId = "001", Password = "pwd" }).AccessToken;
        }

        private SystemBusinessObject CreateBo(Guid accessToken)
            => new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken, SysProgIds.System);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("Login 應寫入未帶公司的種子")]
        public void Login_WritesSeedWithoutCompany()
        {
            var accessToken = LoginAsSeedUser();
            try
            {
                var seed = SessionRepository.GetSession(accessToken);
                Assert.NotNull(seed);
                Assert.Equal(accessToken, seed!.AccessToken);
                Assert.Equal("001", seed.UserID);
                Assert.Null(seed.CompanyId);
                Assert.True(seed.EndTime > DateTime.UtcNow);
            }
            finally
            {
                CreateBo(accessToken).Logout(new LogoutArgs());
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("EnterCompany 與 LeaveCompany 應同步更新種子的 CompanyId")]
        public void EnterAndLeaveCompany_UpdateSeedCompanyId()
        {
            var accessToken = LoginAsSeedUser();
            var bo = CreateBo(accessToken);
            try
            {
                bo.EnterCompany(new EnterCompanyArgs { CompanyId = SeedCompanyId });
                Assert.Equal(SeedCompanyId, SessionRepository.GetSession(accessToken)!.CompanyId);

                bo.LeaveCompany(new LeaveCompanyArgs());
                Assert.Null(SessionRepository.GetSession(accessToken)!.CompanyId);
            }
            finally
            {
                bo.Logout(new LogoutArgs());
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("Logout 應刪除種子，token 不得由資料庫復活")]
        public void Logout_DeletesSeed_TokenCannotBeRevived()
        {
            var accessToken = LoginAsSeedUser();
            var bo = CreateBo(accessToken);
            bo.EnterCompany(new EnterCompanyArgs { CompanyId = SeedCompanyId });

            bo.Logout(new LogoutArgs());

            // 快取與種子都必須消失——只清快取的話，下一個請求就會把 token 重建回來。
            Assert.Null(_fx.GetRequiredService<ISessionInfoService>().Get(accessToken));
            Assert.Null(SessionRepository.GetSession(accessToken));
        }
    }
}
