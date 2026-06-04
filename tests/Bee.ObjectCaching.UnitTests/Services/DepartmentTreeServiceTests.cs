using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;
using Bee.ObjectCaching.Services;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.UnitTests.Services
{
    /// <summary>
    /// <see cref="DepartmentTreeService"/> 建構子防衛與 Get / Remove 邏輯的單元測試。
    /// 依賴注入以 stub 取代，快取以 per-test 唯一 prefix 隔離，不影響其他並行測試。
    /// </summary>
    public class DepartmentTreeServiceTests
    {
        private sealed class StubCompanyInfoService : ICompanyInfoService
        {
            private readonly CompanyInfo? _result;
            public StubCompanyInfoService(CompanyInfo? result) => _result = result;
            public CompanyInfo? Get(string companyId) => _result;
            public void Set(CompanyInfo companyInfo) { }
            public void Remove(string companyId) { }
        }

        private sealed class StubDepartmentRepository : IDepartmentRepository
        {
            private readonly IReadOnlyList<DepartmentNode> _nodes;
            public StubDepartmentRepository(IReadOnlyList<DepartmentNode> nodes) => _nodes = nodes;
            public IReadOnlyList<DepartmentNode> GetDepartments(string databaseId) => _nodes;
        }

        private sealed class MinimalCacheContainer : ICacheContainer
        {
            public DepartmentTreeCache DepartmentTree { get; }

            public MinimalCacheContainer(string prefix) => DepartmentTree = new DepartmentTreeCache(prefix);

            public SystemSettingsCache SystemSettings => throw new NotImplementedException();
            public DatabaseSettingsCache DatabaseSettings => throw new NotImplementedException();
            public ProgramSettingsCache ProgramSettings => throw new NotImplementedException();
            public PermissionModelsCache PermissionModels => throw new NotImplementedException();
            public DbCategorySettingsCache DbCategorySettings => throw new NotImplementedException();
            public TableSchemaCache TableSchema => throw new NotImplementedException();
            public FormSchemaCache FormSchema => throw new NotImplementedException();
            public FormLayoutCache FormLayout => throw new NotImplementedException();
            public LanguageResourceCache LanguageResource => throw new NotImplementedException();
            public SessionInfoCache SessionInfo => throw new NotImplementedException();
            public CompanyInfoCache CompanyInfo => throw new NotImplementedException();
            public CompanyRolePermissionsCache CompanyRolePermissions => throw new NotImplementedException();
            public bool TryEvict(string cacheKey) => false;
        }

        private static readonly IReadOnlyList<DepartmentNode> s_emptyNodes = [];

        private static MinimalCacheContainer NewContainer() =>
            new MinimalCacheContainer(Guid.NewGuid().ToString("N"));

        [Fact]
        [DisplayName("DepartmentTreeService 建構子 cache 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullCache_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DepartmentTreeService(
                    null!,
                    new StubCompanyInfoService(null),
                    new StubDepartmentRepository(s_emptyNodes)));
        }

        [Fact]
        [DisplayName("DepartmentTreeService 建構子 companyInfoService 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullCompanyInfoService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DepartmentTreeService(
                    NewContainer(),
                    null!,
                    new StubDepartmentRepository(s_emptyNodes)));
        }

        [Fact]
        [DisplayName("DepartmentTreeService 建構子 repository 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DepartmentTreeService(
                    NewContainer(),
                    new StubCompanyInfoService(null),
                    null!));
        }

        [Fact]
        [DisplayName("Get 快取命中時應直接回傳快取的樹狀結構，不呼叫 repository")]
        public void Get_CacheHit_ReturnsCachedTree()
        {
            var container = NewContainer();
            var tree = new DepartmentTree("C001", s_emptyNodes);
            container.DepartmentTree.Set(tree);

            var service = new DepartmentTreeService(
                container,
                new StubCompanyInfoService(null),
                new StubDepartmentRepository(s_emptyNodes));

            var result = service.Get("C001");

            Assert.NotNull(result);
            Assert.Equal("C001", result.CompanyId);
        }

        [Fact]
        [DisplayName("Get 快取未命中且公司不存在時應回傳 null")]
        public void Get_CompanyNotFound_ReturnsNull()
        {
            var service = new DepartmentTreeService(
                NewContainer(),
                new StubCompanyInfoService(null),
                new StubDepartmentRepository(s_emptyNodes));

            var result = service.Get("UNKNOWN");

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("Get 快取未命中且公司存在時應從 repository 建立樹並寫入快取")]
        public void Get_CacheMissAndCompanyFound_BuildsAndCachesTree()
        {
            var container = NewContainer();
            var company = new CompanyInfo { CompanyId = "C002", CompanyDatabaseId = "company_db" };
            var node = new DepartmentNode(Guid.NewGuid(), "DEPT1", "部門一", Guid.Empty, Guid.Empty);

            var service = new DepartmentTreeService(
                container,
                new StubCompanyInfoService(company),
                new StubDepartmentRepository([node]));

            var result = service.Get("C002");

            Assert.NotNull(result);
            Assert.Equal("C002", result.CompanyId);
            Assert.Single(result.Nodes!);
            Assert.NotNull(container.DepartmentTree.Get("C002"));
        }

        [Fact]
        [DisplayName("Remove 應將指定公司的樹狀結構從快取移除")]
        public void Remove_DelegatesToCache_EvictsEntry()
        {
            var container = NewContainer();
            var tree = new DepartmentTree("C003", s_emptyNodes);
            container.DepartmentTree.Set(tree);

            var service = new DepartmentTreeService(
                container,
                new StubCompanyInfoService(null),
                new StubDepartmentRepository(s_emptyNodes));
            service.Remove("C003");

            Assert.Null(container.DepartmentTree.Get("C003"));
        }
    }
}
