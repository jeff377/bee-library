using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Identity;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="RepositoryDatabaseRouter"/> 行為測試。Stub 化 ISessionInfoService /
    /// ICompanyInfoService 不需實體 DB；專注驗證 DbScope → databaseId 解析路徑與
    /// 錯誤分支。
    /// </summary>
    public class RepositoryDatabaseRouterTests
    {
        #region Stubs

        private sealed class StubSessionInfoService : ISessionInfoService
        {
            private readonly Dictionary<Guid, SessionInfo> _store = [];

            public SessionInfo Get(Guid accessToken)
                => _store.TryGetValue(accessToken, out var info) ? info : null!;

            public void Set(SessionInfo sessionInfo) => _store[sessionInfo.AccessToken] = sessionInfo;

            public void Remove(Guid accessToken) => _store.Remove(accessToken);
        }

        private sealed class StubCompanyInfoService : ICompanyInfoService
        {
            private readonly Dictionary<string, CompanyInfo> _store = [];

            public CompanyInfo? Get(string companyId)
                => _store.TryGetValue(companyId, out var info) ? info : null;

            public void Set(CompanyInfo companyInfo) => _store[companyInfo.CompanyId] = companyInfo;

            public void Remove(string companyId) => _store.Remove(companyId);
        }

        private static (RepositoryDatabaseRouter router, StubSessionInfoService sessions, StubCompanyInfoService companies) NewRouter()
        {
            var sessions = new StubSessionInfoService();
            var companies = new StubCompanyInfoService();
            return (new RepositoryDatabaseRouter(sessions, companies), sessions, companies);
        }

        #endregion

        [Fact]
        [DisplayName("Resolve(Common) 應回固定 \"common\"，不需 session")]
        public void Resolve_Common_ReturnsCommon()
        {
            var (router, _, _) = NewRouter();
            Assert.Equal(DbCategoryIds.Common, router.Resolve(DbScope.Common, Guid.Empty));
        }

        [Fact]
        [DisplayName("Resolve(Log) 應回固定 \"log\"，不需 session（支援 pre-EnterCompany 寫 log）")]
        public void Resolve_Log_ReturnsLog()
        {
            var (router, _, _) = NewRouter();
            Assert.Equal(DbCategoryIds.Log, router.Resolve(DbScope.Log, Guid.Empty));
        }

        [Fact]
        [DisplayName("Resolve(Common/Log) 帶 Guid.Empty 仍能回固定 databaseId")]
        public void Resolve_CommonAndLogWithEmptyAccessToken_ReturnsFixedDatabaseId()
        {
            var (router, _, _) = NewRouter();
            Assert.Equal(DbCategoryIds.Common, router.Resolve(DbScope.Common, Guid.Empty));
            Assert.Equal(DbCategoryIds.Log, router.Resolve(DbScope.Log, Guid.Empty));
        }

        [Fact]
        [DisplayName("Resolve(Company) 在 session 與 CompanyInfo 齊備時應回 CompanyDatabaseId")]
        public void Resolve_CompanyWithSession_ReturnsCompanyDatabaseId()
        {
            var (router, sessions, companies) = NewRouter();
            var token = Guid.NewGuid();
            sessions.Set(new SessionInfo { AccessToken = token, UserId = "u", CompanyId = "C001" });
            companies.Set(new CompanyInfo { CompanyId = "C001", CompanyDatabaseId = "biz_shared_01" });

            Assert.Equal("biz_shared_01", router.Resolve(DbScope.Company, token));
        }

        [Fact]
        [DisplayName("Resolve(Company) 在 session 不存在時應拋 UnauthorizedAccessException")]
        public void Resolve_CompanyNoSession_ThrowsUnauthorized()
        {
            var (router, _, _) = NewRouter();
            Assert.Throws<UnauthorizedAccessException>(
                () => router.Resolve(DbScope.Company, Guid.NewGuid()));
        }

        [Fact]
        [DisplayName("Resolve(Company) 在 session 未進公司時應拋 CompanyNotEntered")]
        public void Resolve_CompanySessionWithoutCompanyId_ThrowsCompanyNotEntered()
        {
            var (router, sessions, _) = NewRouter();
            var token = Guid.NewGuid();
            sessions.Set(new SessionInfo { AccessToken = token, UserId = "u", CompanyId = null });

            var ex = Assert.Throws<InvalidOperationException>(
                () => router.Resolve(DbScope.Company, token));
            Assert.Equal("CompanyNotEntered", ex.Message);
        }

        [Fact]
        [DisplayName("Resolve(Company) 在 CompanyInfo cache miss 時應拋 InvalidOperationException 且訊息不含 CompanyId")]
        public void Resolve_CompanyInfoCacheMiss_ThrowsAndDoesNotLeakCompanyId()
        {
            var (router, sessions, _) = NewRouter();
            var token = Guid.NewGuid();
            sessions.Set(new SessionInfo { AccessToken = token, UserId = "u", CompanyId = "SECRET_C001" });

            var ex = Assert.Throws<InvalidOperationException>(
                () => router.Resolve(DbScope.Company, token));
            Assert.DoesNotContain("SECRET_C001", ex.Message);
        }

        [Fact]
        [DisplayName("Resolve(Company) 多公司指向同一 CompanyDatabaseId 都應正確回傳該 id")]
        public void Resolve_TwoCompaniesWithSameCompanyDatabaseId_BothReturnSameDatabaseId()
        {
            var (router, sessions, companies) = NewRouter();
            companies.Set(new CompanyInfo { CompanyId = "CA", CompanyDatabaseId = "biz_shared" });
            companies.Set(new CompanyInfo { CompanyId = "CB", CompanyDatabaseId = "biz_shared" });

            var tokenA = Guid.NewGuid();
            var tokenB = Guid.NewGuid();
            sessions.Set(new SessionInfo { AccessToken = tokenA, UserId = "ua", CompanyId = "CA" });
            sessions.Set(new SessionInfo { AccessToken = tokenB, UserId = "ub", CompanyId = "CB" });

            Assert.Equal("biz_shared", router.Resolve(DbScope.Company, tokenA));
            Assert.Equal("biz_shared", router.Resolve(DbScope.Company, tokenB));
        }

        [Fact]
        [DisplayName("Ctor 傳入 null services 應拋 ArgumentNullException")]
        public void Ctor_NullServices_ThrowsArgumentNullException()
        {
            var companies = new StubCompanyInfoService();
            var sessions = new StubSessionInfoService();
            Assert.Throws<ArgumentNullException>(
                () => new RepositoryDatabaseRouter(null!, companies));
            Assert.Throws<ArgumentNullException>(
                () => new RepositoryDatabaseRouter(sessions, null!));
        }
    }
}
