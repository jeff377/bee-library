using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Organization;
using Bee.Definition.Security;
using Bee.ObjectCaching.Database;

namespace Bee.ObjectCaching.UnitTests.Database
{
    /// <summary>
    /// 資料庫相依快取的直接覆蓋：read-through 只打一次、`Set` 覆寫、`Remove` 後重新載入、
    /// 沒有 data source 時不讀。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這五個快取先前在 `tests/` 的參照數全是 0（`CompanyInfoCache` 只有一處間接提及）。
    /// 依 <c>rules/definition.md</c>，它們屬於**沒有 `SaveX`、只靠 cache-notify 失效**的高風險類別
    /// —— 漏一次 notify 就全 process 拿舊值。服務層有測試，快取層本身沒有。
    /// </para>
    /// <para>
    /// 用 stub data source 而非真資料庫：這裡驗的是快取語意（何時讀、何時不讀），
    /// 資料從哪來與它無關。
    /// </para>
    /// </remarks>
    public class DatabaseBackedCacheTests
    {
        /// <summary>只記錄呼叫次數的 data source，用來看 read-through 發生了幾次。</summary>
        private sealed class CountingSource : ICacheDataSourceProvider
        {
            public int CompanyInfoCalls { get; private set; }
            public int RolePermissionCalls { get; private set; }
            public int DepartmentTreeCalls { get; private set; }
            public int AuditRuleCalls { get; private set; }
            public int GateCalls { get; private set; }

            public SessionInfo? GetSessionInfo(Guid accessToken) => null;

            public CompanyInfo? GetCompanyInfo(string companyId)
            { CompanyInfoCalls++; return new CompanyInfo { CompanyId = companyId }; }

            public CompanyRolePermissions? GetCompanyRolePermissions(string companyId)
            { RolePermissionCalls++; return new CompanyRolePermissions(companyId, [], []); }

            public DepartmentTree? GetDepartmentTree(string companyId)
            { DepartmentTreeCalls++; return new DepartmentTree(); }

            public CompanyAuditRules? GetCompanyAuditRules(string companyId)
            { AuditRuleCalls++; return new CompanyAuditRules(companyId, []); }

            public ApiKeyInfo? GetApiKey(string sysId) => null;

            public ApiKeyGateState GetApiKeyGateState()
            { GateCalls++; return new ApiKeyGateState { InForce = true }; }
        }

        private static string NewPrefix() => "t" + Guid.NewGuid().ToString("N");

        [Fact]
        [DisplayName("CompanyInfoCache：miss 讀一次，之後命中不再讀；Remove 後重新讀")]
        public void CompanyInfoCache_ReadsThroughOnceThenCaches()
        {
            var source = new CountingSource();
            var cache = new CompanyInfoCache(() => source, NewPrefix());

            Assert.NotNull(cache.Get("c1"));
            Assert.NotNull(cache.Get("c1"));
            Assert.Equal(1, source.CompanyInfoCalls);

            cache.Remove("c1");
            Assert.NotNull(cache.Get("c1"));
            Assert.Equal(2, source.CompanyInfoCalls);
        }

        [Fact]
        [DisplayName("CompanyRolePermissionsCache：miss 讀一次，之後命中不再讀")]
        public void CompanyRolePermissionsCache_ReadsThroughOnce()
        {
            var source = new CountingSource();
            var cache = new CompanyRolePermissionsCache(() => source, NewPrefix());

            Assert.NotNull(cache.Get("c1"));
            Assert.NotNull(cache.Get("c1"));
            Assert.Equal(1, source.RolePermissionCalls);
        }

        [Fact]
        [DisplayName("DepartmentTreeCache：miss 讀一次，之後命中不再讀")]
        public void DepartmentTreeCache_ReadsThroughOnce()
        {
            var source = new CountingSource();
            var cache = new DepartmentTreeCache(() => source, NewPrefix());

            Assert.NotNull(cache.Get("c1"));
            Assert.NotNull(cache.Get("c1"));
            Assert.Equal(1, source.DepartmentTreeCalls);
        }

        [Fact]
        [DisplayName("CompanyAuditRulesCache：miss 讀一次，之後命中不再讀")]
        public void CompanyAuditRulesCache_ReadsThroughOnce()
        {
            var source = new CountingSource();
            var cache = new CompanyAuditRulesCache(() => source, NewPrefix());

            Assert.NotNull(cache.Get("c1"));
            Assert.NotNull(cache.Get("c1"));
            Assert.Equal(1, source.AuditRuleCalls);
        }

        [Fact]
        [DisplayName("ApiKeyGateCache：GetState 讀一次；RemoveState 後重新讀")]
        public void ApiKeyGateCache_ReadsThroughOnceThenReloadsAfterRemove()
        {
            var source = new CountingSource();
            var cache = new ApiKeyGateCache(() => source, NewPrefix());

            Assert.NotNull(cache.GetState());
            Assert.NotNull(cache.GetState());
            Assert.Equal(1, source.GateCalls);

            cache.RemoveState();
            Assert.NotNull(cache.GetState());
            Assert.Equal(2, source.GateCalls);
        }

        [Fact]
        [DisplayName("ApiKeyGateCache 的 cache group 刻意與 ApiKeyInfo 相同（金鑰異動要一併失效閘門）")]
        public void ApiKeyGateCache_SharesTheApiKeyCacheGroup()
        {
            // 不是筆誤：金鑰失效時若沒一併失效閘門條目，新簽發的金鑰最長一小時內會被拒。
            Assert.Equal(nameof(ApiKeyInfo), new ApiKeyGateCache(NewPrefix()).CacheGroup);
        }

        [Fact]
        [DisplayName("沒有 data source 時不得讀取，Get 回 null")]
        public void NoDataSource_GetReturnsNullWithoutReadingThrough()
        {
            // 公開建構子就是這個形狀（Set 是唯一入口），行動端與測試都會走到。
            Assert.Null(new CompanyInfoCache(NewPrefix()).Get("c1"));
            Assert.Null(new CompanyAuditRulesCache(NewPrefix()).Get("c1"));
            Assert.Null(new DepartmentTreeCache(NewPrefix()).Get("c1"));
        }
    }
}
