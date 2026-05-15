using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.ObjectCaching.Services;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="CompanyInfoService"/> 行為測試。每個測試自建獨立的
    /// <see cref="CacheContainerService"/>（不共用 process-wide cache），可與其他 test class 平行執行。
    /// </summary>
    public class CompanyInfoServiceTests
    {
        private sealed class StubCompanyRepository : ICompanyRepository
        {
            private readonly Func<string, CompanyInfo?> _resolver;
            public int GetByIdCallCount { get; private set; }
            public StubCompanyRepository() : this(_ => null) { }
            public StubCompanyRepository(Func<string, CompanyInfo?> resolver) { _resolver = resolver; }
            public CompanyInfo? GetById(string companyId)
            {
                GetByIdCallCount++;
                return _resolver(companyId);
            }
        }

        private static CompanyInfoService NewService(out CacheContainerService container, ICompanyRepository? repo = null)
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var storage = new Bee.Definition.Storage.FileDefineStorage(paths);
            container = new CacheContainerService(storage, paths, "company_svc_" + Guid.NewGuid().ToString("N"));
            return new CompanyInfoService(container, repo ?? new StubCompanyRepository());
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
        [DisplayName("Get 不存在且 repository 也無資料時應回 null")]
        public void Get_MissingCompanyId_RepoEmpty_ReturnsNull()
        {
            var service = NewService(out _);
            Assert.Null(service.Get("UNKNOWN"));
        }

        [Fact]
        [DisplayName("Get cache miss 時應呼叫 repository fallback，並把結果寫回 cache")]
        public void Get_CacheMiss_LoadsFromRepository_AndPopulatesCache()
        {
            var repo = new StubCompanyRepository(id => id == "DB_ONLY"
                ? new CompanyInfo { CompanyId = "DB_ONLY", CompanyName = "from-db", CompanyDatabaseId = "common" }
                : null);
            var service = NewService(out var container, repo);

            var first = service.Get("DB_ONLY");
            Assert.NotNull(first);
            Assert.Equal("from-db", first.CompanyName);
            Assert.Equal(1, repo.GetByIdCallCount);

            // 第二次應命中 cache，不再打 repository
            var second = service.Get("DB_ONLY");
            Assert.NotNull(second);
            Assert.Equal(1, repo.GetByIdCallCount);
        }

        [Fact]
        [DisplayName("Ctor 傳入 null cache 應拋例外")]
        public void Ctor_NullCache_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new CompanyInfoService(null!, new StubCompanyRepository()));
        }

        [Fact]
        [DisplayName("Ctor 傳入 null companyRepository 應拋例外")]
        public void Ctor_NullCompanyRepository_Throws()
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var storage = new Bee.Definition.Storage.FileDefineStorage(paths);
            var cache = new CacheContainerService(storage, paths, "company_svc_null_repo_" + Guid.NewGuid().ToString("N"));
            Assert.Throws<ArgumentNullException>(() => new CompanyInfoService(cache, null!));
        }
    }
}
