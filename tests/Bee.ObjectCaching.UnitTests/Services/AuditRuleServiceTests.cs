using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Organization;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.ObjectCaching.Services;

namespace Bee.ObjectCaching.UnitTests.Services
{
    /// <summary>
    /// <see cref="AuditRuleService"/> 的單元測試。每個測試使用獨立的
    /// <see cref="CacheContainerService"/>（唯一 prefix），可與其他 test class 平行執行。
    /// </summary>
    public class AuditRuleServiceTests
    {
        private sealed class StubCacheDataSourceProvider : ICacheDataSourceProvider
        {
            private readonly Func<string, CompanyAuditRules?> _resolver;
            public int GetCompanyAuditRulesCallCount { get; private set; }
            public StubCacheDataSourceProvider(Func<string, CompanyAuditRules?> resolver) { _resolver = resolver; }

            public CompanyAuditRules? GetCompanyAuditRules(string companyId)
            {
                GetCompanyAuditRulesCallCount++;
                return _resolver(companyId);
            }

            public SessionInfo? GetSessionInfo(Guid accessToken) => null;
            public CompanyInfo? GetCompanyInfo(string companyId) => null;
            public CompanyRolePermissions? GetCompanyRolePermissions(string companyId) => null;
            public DepartmentTree? GetDepartmentTree(string companyId) => null;
            public ApiKeyInfo? GetApiKey(string sysId) => null;
            public ApiKeyGateState GetApiKeyGateState() => new();
        }

        private static CacheContainerService NewCache(ICacheDataSourceProvider dataSource)
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var storage = new FileDefineStorage(paths);
            string prefix = "audit_rule_svc_" + Guid.NewGuid().ToString("N");
            return new CacheContainerService(storage, paths, prefix, () => dataSource);
        }

        private static CompanyAuditRules RulesFor(string companyId)
            => new(companyId, [new AuditRule("Order", AuditRuleMode.On, AuditRuleMode.Off, true)]);

        [Fact]
        [DisplayName("Get 應於快取未命中時讀穿資料來源並回傳快照")]
        public void Get_CacheMiss_ReadsThroughDataSource()
        {
            var stub = new StubCacheDataSourceProvider(RulesFor);
            var service = new AuditRuleService(NewCache(stub));

            var rules = service.Get("C001");

            Assert.NotNull(rules);
            Assert.Equal("C001", rules.CompanyId);
            Assert.Equal(AuditRuleMode.On, rules.Find("Order")!.ChangeMode);
            Assert.Equal(1, stub.GetCompanyAuditRulesCallCount);
        }

        [Fact]
        [DisplayName("Get 第二次應命中快取，不再讀資料來源")]
        public void Get_SecondCall_DoesNotHitDataSource()
        {
            var stub = new StubCacheDataSourceProvider(RulesFor);
            var service = new AuditRuleService(NewCache(stub));

            service.Get("C001");
            service.Get("C001");

            Assert.Equal(1, stub.GetCompanyAuditRulesCallCount);
        }

        [Fact]
        [DisplayName("Remove 後再取應重新讀穿資料來源")]
        public void Remove_ThenGet_ReloadsFromDataSource()
        {
            var stub = new StubCacheDataSourceProvider(RulesFor);
            var service = new AuditRuleService(NewCache(stub));

            service.Get("C001");
            service.Remove("C001");
            service.Get("C001");

            Assert.Equal(2, stub.GetCompanyAuditRulesCallCount);
        }

        [Fact]
        [DisplayName("不同公司應各自快取，不互相污染")]
        public void Get_DifferentCompanies_CachedSeparately()
        {
            var stub = new StubCacheDataSourceProvider(RulesFor);
            var service = new AuditRuleService(NewCache(stub));

            var first = service.Get("C001");
            var second = service.Get("C002");

            Assert.Equal("C001", first!.CompanyId);
            Assert.Equal("C002", second!.CompanyId);
            Assert.Equal(2, stub.GetCompanyAuditRulesCallCount);
        }

        [Fact]
        [DisplayName("資料來源回 null（公司不存在）時 Get 應回 null")]
        public void Get_UnknownCompany_ReturnsNull()
        {
            var stub = new StubCacheDataSourceProvider(_ => null);
            var service = new AuditRuleService(NewCache(stub));

            Assert.Null(service.Get("nope"));
        }
    }
}
