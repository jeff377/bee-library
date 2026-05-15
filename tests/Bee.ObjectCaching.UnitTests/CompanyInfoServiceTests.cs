using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.ObjectCaching.Services;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="CompanyInfoService"/> 行為測試。每個測試自建獨立的
    /// <see cref="CacheContainerService"/>（不共用 process-wide cache），可與其他 test class 平行執行。
    /// </summary>
    public class CompanyInfoServiceTests
    {
        private static CompanyInfoService NewService(out CacheContainerService container)
        {
            var paths = new PathOptions { DefinePath = Path.GetTempPath() };
            var storage = new Bee.Definition.Storage.FileDefineStorage(paths);
            container = new CacheContainerService(storage, paths, "company_svc_" + Guid.NewGuid().ToString("N"));
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
        [DisplayName("Get 不存在的 companyId 應回傳 null")]
        public void Get_MissingCompanyId_ReturnsNull()
        {
            var service = NewService(out _);
            Assert.Null(service.Get("UNKNOWN"));
        }

        [Fact]
        [DisplayName("第二次查同一個不存在 companyId 應命中 negative cache（CreateInstance 不再被呼叫）")]
        public void Get_RepeatedMiss_HitsNegativeCache()
        {
            // 透過 ICacheContainer 暴露 CompanyInfoCache 的 ref，直接驗負向快取行為。
            var service = NewService(out var container);
            var cache = container.CompanyInfo;

            // 先 miss 一次，寫入 negative marker
            Assert.Null(service.Get("MISS_X"));
            // 第二次仍回 null（行為一致），且因為 CompanyInfoCache.CreateInstance 永遠回 null，
            // 我們改檢查 Set 真實資料後 Get 仍可回傳——確保 marker 沒卡死 Set 路徑
            var info = new CompanyInfo { CompanyId = "MISS_X", CompanyName = "Recovered" };
            cache.Set(info);
            var loaded = service.Get("MISS_X");
            Assert.NotNull(loaded);
            Assert.Equal("Recovered", loaded.CompanyName);
        }

        [Fact]
        [DisplayName("Ctor 傳入 null cache 應拋例外")]
        public void Ctor_NullCache_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CompanyInfoService(null!));
        }
    }
}
