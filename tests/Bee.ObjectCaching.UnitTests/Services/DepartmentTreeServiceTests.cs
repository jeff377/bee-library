using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.Definition.Storage;
using Bee.ObjectCaching.Services;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.UnitTests.Services
{
    /// <summary>
    /// <see cref="DepartmentTreeService"/> 的建構子防衛、Get 快取命中/未命中、
    /// 以及 Remove 行為的單元測試。每個測試使用獨立快取前綴，可平行執行。
    /// </summary>
    public class DepartmentTreeServiceTests
    {
        private sealed class StubCompanyInfoService : ICompanyInfoService
        {
            private readonly Func<string, CompanyInfo?> _resolver;

            public StubCompanyInfoService(Func<string, CompanyInfo?>? resolver = null)
                => _resolver = resolver ?? (_ => null);

            public CompanyInfo? Get(string companyId) => _resolver(companyId);
            public void Set(CompanyInfo companyInfo) { }
            public void Remove(string companyId) { }
        }

        private sealed class StubDepartmentRepository : IDepartmentRepository
        {
            private readonly IReadOnlyList<DepartmentNode> _nodes;

            public int GetDepartmentsCallCount { get; private set; }

            public StubDepartmentRepository(IReadOnlyList<DepartmentNode>? nodes = null)
                => _nodes = nodes ?? [];

            public IReadOnlyList<DepartmentNode> GetDepartments(string databaseId)
            {
                GetDepartmentsCallCount++;
                return _nodes;
            }
        }

        private static (DepartmentTreeService Service, ICacheContainer Container) NewService(
            ICompanyInfoService? companyInfoService = null,
            IDepartmentRepository? repository = null)
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var storage = new FileDefineStorage(paths);
            var cache = new CacheContainerService(storage, paths, "deptree_" + Guid.NewGuid().ToString("N"));
            var service = new DepartmentTreeService(
                cache,
                companyInfoService ?? new StubCompanyInfoService(),
                repository ?? new StubDepartmentRepository());
            return (service, cache);
        }

        [Fact]
        [DisplayName("建構子 cache 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullCache_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DepartmentTreeService(null!, new StubCompanyInfoService(), new StubDepartmentRepository()));
        }

        [Fact]
        [DisplayName("建構子 companyInfoService 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullCompanyInfoService_ThrowsArgumentNullException()
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var cache = new CacheContainerService(new FileDefineStorage(paths), paths, Guid.NewGuid().ToString("N"));
            Assert.Throws<ArgumentNullException>(() =>
                new DepartmentTreeService(cache, null!, new StubDepartmentRepository()));
        }

        [Fact]
        [DisplayName("建構子 repository 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullRepository_ThrowsArgumentNullException()
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var cache = new CacheContainerService(new FileDefineStorage(paths), paths, Guid.NewGuid().ToString("N"));
            Assert.Throws<ArgumentNullException>(() =>
                new DepartmentTreeService(cache, new StubCompanyInfoService(), null!));
        }

        [Fact]
        [DisplayName("Get 快取命中時應直接回傳快取樹，不呼叫 repository")]
        public void Get_CacheHit_ReturnsCachedTreeWithoutCallingRepository()
        {
            var repo = new StubDepartmentRepository();
            var (service, container) = NewService(repository: repo);
            string companyId = "C_HIT_" + Guid.NewGuid().ToString("N");
            var expected = new DepartmentTree(companyId, []);
            container.DepartmentTree.Set(expected);

            var result = service.Get(companyId);

            Assert.Same(expected, result);
            Assert.Equal(0, repo.GetDepartmentsCallCount);
        }

        [Fact]
        [DisplayName("Get 快取未命中且公司不存在時應回傳 null")]
        public void Get_CacheMiss_CompanyNotFound_ReturnsNull()
        {
            var (service, _) = NewService();

            var result = service.Get("UNKNOWN_" + Guid.NewGuid().ToString("N"));

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("Get 快取未命中且公司存在時應建立樹並寫入快取後回傳")]
        public void Get_CacheMiss_CompanyFound_BuildsAndCachesTree()
        {
            string companyId = "C_MISS_" + Guid.NewGuid().ToString("N");
            var company = new CompanyInfo { CompanyId = companyId, CompanyDatabaseId = "biz_db" };
            var companyInfoService = new StubCompanyInfoService(id => id == companyId ? company : null);
            var nodes = new List<DepartmentNode>
            {
                new DepartmentNode(Guid.NewGuid(), "D1", "部門一", Guid.Empty, Guid.Empty)
            };
            var repo = new StubDepartmentRepository(nodes);
            var (service, container) = NewService(companyInfoService, repo);

            var result = service.Get(companyId);

            Assert.NotNull(result);
            Assert.Equal(companyId, result.CompanyId);
            Assert.Equal(1, repo.GetDepartmentsCallCount);
            Assert.NotNull(container.DepartmentTree.Get(companyId));
        }

        [Fact]
        [DisplayName("Get 快取未命中後第二次呼叫應命中快取，不再呼叫 repository")]
        public void Get_SecondCall_HitsCache()
        {
            string companyId = "C_2ND_" + Guid.NewGuid().ToString("N");
            var company = new CompanyInfo { CompanyId = companyId, CompanyDatabaseId = "biz_db" };
            var companyInfoService = new StubCompanyInfoService(id => id == companyId ? company : null);
            var repo = new StubDepartmentRepository();
            var (service, _) = NewService(companyInfoService, repo);

            service.Get(companyId);
            service.Get(companyId);

            Assert.Equal(1, repo.GetDepartmentsCallCount);
        }

        [Fact]
        [DisplayName("Remove 應將公司的部門樹從快取中移除")]
        public void Remove_RemovesTreeFromCache()
        {
            string companyId = "C_REM_" + Guid.NewGuid().ToString("N");
            var (service, container) = NewService();
            var tree = new DepartmentTree(companyId, []);
            container.DepartmentTree.Set(tree);
            Assert.NotNull(container.DepartmentTree.Get(companyId));

            service.Remove(companyId);

            Assert.Null(container.DepartmentTree.Get(companyId));
        }
    }
}
