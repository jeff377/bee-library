using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// Unit tests for the convention-based eviction dispatch on <see cref="ICacheContainer.TryEvict"/>
    /// (no database). A bumped <c>"group:entity"</c> key is routed to the owned cache whose
    /// <see cref="IEvictableCache.CacheGroup"/> matches the group; unknown groups are ignored.
    /// </summary>
    public class CacheContainerTryEvictTests : IClassFixture<BeeTestFixture>
    {
        private readonly ICacheContainer _container;

        public CacheContainerTryEvictTests(BeeTestFixture fx)
        {
            _container = fx.GetRequiredService<ICacheContainer>();
        }

        [Fact]
        [DisplayName("TryEvict 依群組分派到 keyed cache 並移除該實體")]
        public void TryEvict_KeyedCache_RemovesEntity()
        {
            string companyId = "TE_" + Guid.NewGuid().ToString("N");
            _container.CompanyInfo.Set(new CompanyInfo { CompanyId = companyId, CompanyName = "x" });
            Assert.NotNull(_container.CompanyInfo.Get(companyId));

            bool evicted = _container.TryEvict($"{nameof(CompanyInfo)}:{companyId}");

            Assert.True(evicted);
            Assert.Null(_container.CompanyInfo.Get(companyId));
        }

        [Fact]
        [DisplayName("TryEvict 群組比對不分大小寫")]
        public void TryEvict_GroupIsCaseInsensitive()
        {
            string companyId = "TE_" + Guid.NewGuid().ToString("N");
            _container.CompanyInfo.Set(new CompanyInfo { CompanyId = companyId, CompanyName = "x" });

            bool evicted = _container.TryEvict($"companyinfo:{companyId}");

            Assert.True(evicted);
            Assert.Null(_container.CompanyInfo.Get(companyId));
        }

        [Fact]
        [DisplayName("TryEvict 無對應快取的群組回傳 false")]
        public void TryEvict_UnknownGroup_ReturnsFalse()
        {
            Assert.False(_container.TryEvict("NoSuchGroup:anything"));
        }

        [Fact]
        [DisplayName("TryEvict 沒有冒號的 key 回傳 false")]
        public void TryEvict_KeyWithoutSeparator_ReturnsFalse()
        {
            Assert.False(_container.TryEvict(nameof(CompanyInfo)));
        }

        [Fact]
        [DisplayName("TryEvict 對單物件快取忽略 entity、整體清除")]
        public void TryEvict_SingleObjectCache_RemovesWholeEntry()
        {
            // DbCategorySettings is a single-object cache; group = type name. Any "group:*" bump
            // clears it. With no value cached the eviction is a no-op but must still report routed.
            bool evicted = _container.TryEvict($"{nameof(Bee.Definition.Settings.DbCategorySettings)}:*");

            Assert.True(evicted);
        }
    }
}
