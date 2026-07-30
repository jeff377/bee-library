using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.ObjectCaching.Services;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="CompanyInfoService"/> 行為測試。每個測試自建獨立的
    /// <see cref="CacheContainerService"/>（不共用 process-wide cache），可與其他 test class 平行執行。
    /// </summary>
    public class CompanyInfoServiceTests
    {
        private sealed class StubCacheDataSourceProvider : ICacheDataSourceProvider
        {
            private readonly Func<string, CompanyInfo?> _resolver;
            public int GetCompanyInfoCallCount { get; private set; }
            public StubCacheDataSourceProvider() : this(_ => null) { }
            public StubCacheDataSourceProvider(Func<string, CompanyInfo?> resolver) { _resolver = resolver; }

            public CompanyInfo? GetCompanyInfo(string companyId)
            {
                GetCompanyInfoCallCount++;
                return _resolver(companyId);
            }

            public SessionInfo? GetSessionInfo(Guid accessToken) => null;
            public CompanyRolePermissions? GetCompanyRolePermissions(string companyId) => null;
            public DepartmentTree? GetDepartmentTree(string companyId) => null;
        }

        private static CompanyInfoService NewService(out CacheContainerService container,
            StubCacheDataSourceProvider? dataSource = null)
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var storage = new Bee.Definition.Storage.FileDefineStorage(paths);
            var provider = dataSource ?? new StubCacheDataSourceProvider();
            container = new CacheContainerService(storage, paths,
                "company_svc_" + Guid.NewGuid().ToString("N"), () => provider);
            return new CompanyInfoService(container);
        }

        [Fact]
        [DisplayName("Set/Get/Remove 流程應正確操作 Company 快取")]
        public void Set_Get_Remove_Flow_Works()
        {
            var service = NewService(out _);
            var info = new CompanyInfo
            {
                CompanyId = "C001",
                CompanyName = "Acme",
                CompanyDatabaseId = "biz_shared_01"
            };

            service.Set(info);
            var loaded = service.Get("C001");

            Assert.NotNull(loaded);
            Assert.Equal("C001", loaded.CompanyId);
            Assert.Equal("Acme", loaded.CompanyName);
            Assert.Equal("biz_shared_01", loaded.CompanyDatabaseId);

            service.Remove("C001");
            Assert.Null(service.Get("C001"));
        }

        [Fact]
        [DisplayName("Get 不存在且資料來源也無資料時應回 null")]
        public void Get_MissingCompanyId_DataSourceEmpty_ReturnsNull()
        {
            var service = NewService(out _);
            Assert.Null(service.Get("UNKNOWN"));
        }

        [Fact]
        [DisplayName("Get cache miss 時快取應向資料來源讀取，並把結果寫回 cache")]
        public void Get_CacheMiss_LoadsFromDataSource_AndPopulatesCache()
        {
            var dataSource = new StubCacheDataSourceProvider(id => id == "DB_ONLY"
                ? new CompanyInfo { CompanyId = "DB_ONLY", CompanyName = "from-db", CompanyDatabaseId = "common" }
                : null);
            var service = NewService(out _, dataSource);

            var first = service.Get("DB_ONLY");
            Assert.NotNull(first);
            Assert.Equal("from-db", first.CompanyName);
            Assert.Equal(1, dataSource.GetCompanyInfoCallCount);

            // 第二次應命中 cache，不再打資料來源
            var second = service.Get("DB_ONLY");
            Assert.NotNull(second);
            Assert.Equal(1, dataSource.GetCompanyInfoCallCount);
        }

        [Fact]
        [DisplayName("未提供資料來源時 Get 應回 null（維持既有行為）")]
        public void Get_NoDataSource_ReturnsNull()
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var storage = new Bee.Definition.Storage.FileDefineStorage(paths);
            var container = new CacheContainerService(storage, paths,
                "company_svc_no_ds_" + Guid.NewGuid().ToString("N"));
            var service = new CompanyInfoService(container);

            Assert.Null(service.Get("ANY"));
        }

        [Fact]
        [DisplayName("Ctor 傳入 null cache 應拋例外")]
        public void Ctor_NullCache_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CompanyInfoService(null!));
        }
    }
}
