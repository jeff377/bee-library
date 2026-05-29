using System.ComponentModel;
using Bee.Definition;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="CacheContainerProvider"/> per-customizeId override container 行為測試：
    /// 同 customizeId 回同一 container、不同 customizeId 隔離。
    /// </summary>
    public class CacheContainerProviderTests
    {
        private static CacheContainerProvider CreateProvider()
            => new(new PathOptions { DefinePath = "/tmp/base", CustomizePath = "/tmp/customize" });

        [Fact]
        [DisplayName("For 同一 customizeId 多次呼叫應回傳同一 container 實例")]
        public void For_SameCustomizeId_ReturnsSameContainer()
        {
            var provider = CreateProvider();

            var first = provider.For("acme");
            var second = provider.For("acme");

            Assert.Same(first, second);
        }

        [Fact]
        [DisplayName("For 不同 customizeId 應回傳不同 container 實例（租戶隔離）")]
        public void For_DifferentCustomizeId_ReturnsDifferentContainers()
        {
            var provider = CreateProvider();

            var acme = provider.For("acme");
            var globex = provider.For("globex");

            Assert.NotSame(acme, globex);
        }

        [Fact]
        [DisplayName("override container 的 CachePrefix 應為 customizeId（物理隔離）")]
        public void For_ContainerUsesCustomizeIdAsCachePrefix()
        {
            var provider = CreateProvider();

            var container = (CacheContainerService)provider.For("acme");

            Assert.Equal("acme", container.CachePrefix);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("For 傳入空 customizeId 應拋出 ArgumentException")]
        public void For_EmptyCustomizeId_ThrowsArgumentException(string customizeId)
        {
            Assert.Throws<ArgumentException>(() => CreateProvider().For(customizeId));
        }

        [Fact]
        [DisplayName("建構子傳入 null paths 應拋出 ArgumentNullException")]
        public void Constructor_NullPaths_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheContainerProvider(null!));
        }
    }
}
