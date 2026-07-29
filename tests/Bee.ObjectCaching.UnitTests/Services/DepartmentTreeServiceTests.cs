using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.Definition.Storage;
using Bee.ObjectCaching.Services;

namespace Bee.ObjectCaching.UnitTests.Services
{
    /// <summary>
    /// <see cref="DepartmentTreeService"/> 的單元測試。每個測試使用獨立的
    /// <see cref="CacheContainerService"/>（唯一 prefix），可與其他 test class 平行執行。
    /// </summary>
    public class DepartmentTreeServiceTests
    {
        private sealed class StubCacheDataSourceProvider : ICacheDataSourceProvider
        {
            private readonly Func<string, DepartmentTree?> _resolver;
            public int GetDepartmentTreeCallCount { get; private set; }
            public StubCacheDataSourceProvider(Func<string, DepartmentTree?> resolver) { _resolver = resolver; }

            public DepartmentTree? GetDepartmentTree(string companyId)
            {
                GetDepartmentTreeCallCount++;
                return _resolver(companyId);
            }

            public SessionUser? GetSessionUser(Guid accessToken) => null;
            public CompanyInfo? GetCompanyInfo(string companyId) => null;
            public CompanyRolePermissions? GetCompanyRolePermissions(string companyId) => null;
        }

        private static CacheContainerService NewCache(ICacheDataSourceProvider? dataSource = null)
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var storage = new FileDefineStorage(paths);
            string prefix = "dept_svc_" + Guid.NewGuid().ToString("N");
            return dataSource == null
                ? new CacheContainerService(storage, paths, prefix)
                : new CacheContainerService(storage, paths, prefix, () => dataSource);
        }

        [Fact]
        [DisplayName("建構子 cache 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullCache_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DepartmentTreeService(null!));
        }

        [Fact]
        [DisplayName("Get 快取命中時應直接回傳快取的 DepartmentTree，不觸發資料來源")]
        public void Get_CacheHit_ReturnsCachedTree()
        {
            var dataSource = new StubCacheDataSourceProvider(
                _ => throw new InvalidOperationException("should not be called"));
            var cache = NewCache(dataSource);
            var companyId = "C001";
            var cachedTree = new DepartmentTree(companyId, []);
            cache.DepartmentTree.Set(cachedTree);

            var service = new DepartmentTreeService(cache);

            var result = service.Get(companyId);

            Assert.Same(cachedTree, result);
            Assert.Equal(0, dataSource.GetDepartmentTreeCallCount);
        }

        [Fact]
        [DisplayName("Get 快取未命中且公司不存在時應回傳 null")]
        public void Get_CacheMiss_CompanyNotFound_ReturnsNull()
        {
            var dataSource = new StubCacheDataSourceProvider(_ => null);
            var cache = NewCache(dataSource);
            var service = new DepartmentTreeService(cache);

            var result = service.Get("MISSING_COMPANY");

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("Get 快取未命中且公司存在時應由資料來源載入並快取，第二次呼叫命中快取")]
        public void Get_CacheMiss_CompanyFound_LoadsAndCachesTree()
        {
            var companyId = "C002";
            var deptRowId = Guid.NewGuid();
            var rows = new[] { new DepartmentRow(deptRowId, "D001", "Sales", Guid.Empty, Guid.Empty) };
            var dataSource = new StubCacheDataSourceProvider(id => new DepartmentTree(id, rows));
            var cache = NewCache(dataSource);
            var service = new DepartmentTreeService(cache);

            var result = service.Get(companyId);

            Assert.NotNull(result);
            Assert.Equal(companyId, result!.CompanyId);
            Assert.NotNull(result.Roots);
            Assert.Single(result.Roots!);
            Assert.Equal(1, dataSource.GetDepartmentTreeCallCount);

            var second = service.Get(companyId);
            Assert.Same(result, second);
            Assert.Equal(1, dataSource.GetDepartmentTreeCallCount);
        }

        [Fact]
        [DisplayName("未提供資料來源時 Get 應回 null（維持既有行為）")]
        public void Get_NoDataSource_ReturnsNull()
        {
            var cache = NewCache();
            var service = new DepartmentTreeService(cache);

            Assert.Null(service.Get("ANY"));
        }

        [Fact]
        [DisplayName("Remove 應從快取中移除指定公司的 DepartmentTree")]
        public void Remove_EvictsFromCache()
        {
            var dataSource = new StubCacheDataSourceProvider(_ => null);
            var cache = NewCache(dataSource);
            var companyId = "C003";
            var tree = new DepartmentTree(companyId, []);
            cache.DepartmentTree.Set(tree);

            var service = new DepartmentTreeService(cache);

            service.Remove(companyId);

            Assert.Null(cache.DepartmentTree.Get(companyId));
        }
    }
}
