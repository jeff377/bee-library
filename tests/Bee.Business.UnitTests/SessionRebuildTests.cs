using System.ComponentModel;
using Bee.Business.System;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// 驗證快取失效後由 <c>st_session</c> 種子重建 SessionInfo 的行為。
    /// </summary>
    /// <remarks>
    /// 重建是「重跑推導」而非「還原快照」：種子只帶 token / 使用者 / 到期 / 公司，
    /// 角色、客製代碼、record scope 一律重算，金鑰則由 provider 重新導出。
    /// 這正是「權限撤銷後不會殘留在舊快照」的來源。
    /// </remarks>
    public class SessionRebuildTests : IClassFixture<SharedDbFixture>
    {
        private const string SeedCompanyId = "C001";
        private readonly SharedDbFixture _fx;

        public SessionRebuildTests(SharedDbFixture fx) { _fx = fx; }

        private ISessionInfoService SessionService => _fx.GetRequiredService<ISessionInfoService>();

        private Guid LoginAsSeedUser()
        {
            var bo = new TestableSystemBusinessObject(
                TestBeeContext.Create(_fx), Guid.Empty, _ => (true, "Seed User"));
            return bo.Login(new LoginArgs { UserId = "001", Password = "pwd" }).AccessToken;
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("快取被清空後應由種子重建出等價的 SessionInfo")]
        public void Get_AfterCacheEviction_RebuildsFromSeed()
        {
            var accessToken = LoginAsSeedUser();
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken);
            try
            {
                bo.EnterCompany(new EnterCompanyArgs { CompanyId = SeedCompanyId });
                var original = SessionService.Get(accessToken);

                // 模擬 20 分鐘 sliding 逐出 / 行程重啟：只清快取，種子仍在
                SessionService.Remove(accessToken);

                var rebuilt = SessionService.Get(accessToken);

                Assert.NotNull(rebuilt);
                Assert.Equal(accessToken, rebuilt!.AccessToken);
                Assert.Equal("001", rebuilt.UserId);
                Assert.Equal(SeedCompanyId, rebuilt.CompanyId);
                // 金鑰由 accessToken 重新導出，與登入時同一把——Encrypted API 因此仍可用
                Assert.Equal(original!.ApiEncryptionKey, rebuilt.ApiEncryptionKey);
                // EnterCompany 快照的 record scope 是重算而非還原
                Assert.Equal(original.UserRowId, rebuilt.UserRowId);
                Assert.Equal(original.Culture, rebuilt.Culture);
                Assert.Equal(original.TimeZone, rebuilt.TimeZone);
            }
            finally
            {
                new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken).Logout(new LogoutArgs());
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("未進公司的 session 清快取後應重建為未進公司狀態")]
        public void Get_AfterCacheEviction_WithoutCompany_RebuildsCompanyLess()
        {
            var accessToken = LoginAsSeedUser();
            try
            {
                SessionService.Remove(accessToken);

                var rebuilt = SessionService.Get(accessToken);

                Assert.NotNull(rebuilt);
                Assert.Null(rebuilt!.CompanyId);
            }
            finally
            {
                new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken).Logout(new LogoutArgs());
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("登出後 token 不得由種子重建復活")]
        public void Get_AfterLogout_DoesNotRebuild()
        {
            var accessToken = LoginAsSeedUser();
            new SystemBusinessObject(TestBeeContext.Create(_fx), accessToken).Logout(new LogoutArgs());

            Assert.Null(SessionService.Get(accessToken));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("不存在的 token 不得重建出 session")]
        public void Get_UnknownToken_ReturnsNull()
        {
            Assert.Null(SessionService.Get(Guid.NewGuid()));
        }
    }
}
