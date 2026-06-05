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
    /// <see cref="DepartmentTreeService"/> 的單元測試。每個測試使用獨立的
    /// <see cref="CacheContainerService"/>（唯一 prefix），可與其他 test class 平行執行。
    /// </summary>
    public class DepartmentTreeServiceTests
    {
        private sealed class StubCompanyInfoService : ICompanyInfoService
        {
            private readonly Func<string, CompanyInfo?> _resolver;
            public StubCompanyInfoService(Func<string, CompanyInfo?> resolver) { _resolver = resolver; }
            public CompanyInfo? Get(string companyId) => _resolver(companyId);
            public void Set(CompanyInfo companyInfo) { }
            public void Remove(string companyId) { }
        }

        private sealed class StubDepartmentRepository : IDepartmentRepository
        {
            private readonly Func<string, IReadOnlyList<DepartmentRow>> _resolver;
            public StubDepartmentRepository(Func<string, IReadOnlyList<DepartmentRow>> resolver) { _resolver = resolver; }
            public IReadOnlyList<DepartmentRow> GetDepartments(string databaseId) => _resolver(databaseId);
        }

        private static ICacheContainer NewCache()
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var storage = new FileDefineStorage(paths);
            return new CacheContainerService(storage, paths, "dept_svc_" + Guid.NewGuid().ToString("N"));
        }

        [Fact]
        [DisplayName("建構子 cache 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullCache_ThrowsArgumentNullException()
        {
            var companyService = new StubCompanyInfoService(_ => null);
            var repo = new StubDepartmentRepository(_ => []);
            Assert.Throws<ArgumentNullException>(() =>
                new DepartmentTreeService(null!, companyService, repo));
        }

        [Fact]
        [DisplayName("建構子 companyInfoService 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullCompanyInfoService_ThrowsArgumentNullException()
        {
            var cache = NewCache();
            var repo = new StubDepartmentRepository(_ => []);
            Assert.Throws<ArgumentNullException>(() =>
                new DepartmentTreeService(cache, null!, repo));
        }

        [Fact]
        [DisplayName("建構子 repository 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullRepository_ThrowsArgumentNullException()
        {
            var cache = NewCache();
            var companyService = new StubCompanyInfoService(_ => null);
            Assert.Throws<ArgumentNullException>(() =>
                new DepartmentTreeService(cache, companyService, null!));
        }

        [Fact]
        [DisplayName("Get 快取命中時應直接回傳快取的 DepartmentTree，不呼叫 companyInfoService")]
        public void Get_CacheHit_ReturnsCachedTree()
        {
            var cache = NewCache();
            var companyId = "C001";
            var cachedTree = new DepartmentTree(companyId, []);
            cache.DepartmentTree.Set(cachedTree);

            var companyService = new StubCompanyInfoService(
                _ => throw new InvalidOperationException("should not be called"));
            var repo = new StubDepartmentRepository(
                _ => throw new InvalidOperationException("should not be called"));
            var service = new DepartmentTreeService(cache, companyService, repo);

            var result = service.Get(companyId);

            Assert.Same(cachedTree, result);
        }

        [Fact]
        [DisplayName("Get 快取未命中且公司不存在時應回傳 null")]
        public void Get_CacheMiss_CompanyNotFound_ReturnsNull()
        {
            var cache = NewCache();
            var companyService = new StubCompanyInfoService(_ => null);
            var repo = new StubDepartmentRepository(_ => []);
            var service = new DepartmentTreeService(cache, companyService, repo);

            var result = service.Get("MISSING_COMPANY");

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("Get 快取未命中且公司存在時應建構 DepartmentTree 存入快取並回傳，第二次呼叫命中快取")]
        public void Get_CacheMiss_CompanyFound_BuildsAndCachesTree()
        {
            var cache = NewCache();
            var companyId = "C002";
            var deptRowId = Guid.NewGuid();
            var company = new CompanyInfo { CompanyId = companyId, CompanyDatabaseId = "biz_db_01" };
            var rows = new[] { new DepartmentRow(deptRowId, "D001", "Sales", Guid.Empty, Guid.Empty) };

            var companyService = new StubCompanyInfoService(_ => company);
            var repo = new StubDepartmentRepository(_ => rows);
            var service = new DepartmentTreeService(cache, companyService, repo);

            var result = service.Get(companyId);

            Assert.NotNull(result);
            Assert.Equal(companyId, result!.CompanyId);
            Assert.NotNull(result.Roots);
            Assert.Single(result.Roots!);

            var second = service.Get(companyId);
            Assert.Same(result, second);
        }

        [Fact]
        [DisplayName("Remove 應從快取中移除指定公司的 DepartmentTree")]
        public void Remove_EvictsFromCache()
        {
            var cache = NewCache();
            var companyId = "C003";
            var tree = new DepartmentTree(companyId, []);
            cache.DepartmentTree.Set(tree);

            var companyService = new StubCompanyInfoService(_ => null);
            var repo = new StubDepartmentRepository(_ => []);
            var service = new DepartmentTreeService(cache, companyService, repo);

            service.Remove(companyId);

            Assert.Null(cache.DepartmentTree.Get(companyId));
        }
    }
}
