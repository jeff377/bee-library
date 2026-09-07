using System.ComponentModel;
using Bee.LoadTests.Caching;
using Bee.ObjectCaching;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="CountingCacheProvider"/>.
    /// </summary>
    public class CountingCacheProviderTests
    {
        private static CountingCacheProvider CreateProvider() => new(new InMemoryCacheProvider());

        [Fact]
        [DisplayName("讀到值計為命中，讀不到計為未命中")]
        public void Get_CountsHitsAndMisses()
        {
            var provider = CreateProvider();
            provider.Set("a", "value", new CacheItemPolicy());

            provider.Get("a");
            provider.Get("missing");

            var counters = provider.Snapshot();
            Assert.Equal(1, counters.Hits);
            Assert.Equal(1, counters.Misses);
            Assert.Equal(2, counters.Reads);
            Assert.Equal(0.5, counters.HitRate);
        }

        [Fact]
        [DisplayName("寫入次數即實際建立次數，可證 single-flight 收斂")]
        public void Set_CountsWrites()
        {
            var provider = CreateProvider();

            provider.Set("a", 1, new CacheItemPolicy());
            provider.Set("b", 2, new CacheItemPolicy());

            Assert.Equal(2, provider.Snapshot().Writes);
        }

        [Fact]
        [DisplayName("完全沒有讀取時命中率為零，需先看 Reads 再解讀")]
        public void HitRate_NoReads_ReturnsZero()
        {
            var counters = CreateProvider().Snapshot();

            Assert.Equal(0, counters.Reads);
            Assert.Equal(0, counters.HitRate);
        }

        [Fact]
        [DisplayName("Reset 歸零所有計數，供 warm-up 後重新計算")]
        public void Reset_ClearsAllCounters()
        {
            var provider = CreateProvider();
            provider.Set("a", 1, new CacheItemPolicy());
            provider.Get("a");
            provider.Get("missing");
            provider.Remove("a");

            provider.Reset();

            var counters = provider.Snapshot();
            Assert.Equal(0, counters.Hits);
            Assert.Equal(0, counters.Misses);
            Assert.Equal(0, counters.Writes);
            Assert.Equal(0, counters.Removals);
        }

        [Fact]
        [DisplayName("Reset 不影響底層快取的內容")]
        public void Reset_DoesNotClearUnderlyingCache()
        {
            var provider = CreateProvider();
            provider.Set("a", 1, new CacheItemPolicy());

            provider.Reset();

            Assert.NotNull(provider.Get("a"));
            Assert.Equal(1, provider.GetCount());
        }

        [Fact]
        [DisplayName("Contains 與 GetCount 直接委派給底層且不計數")]
        public void PassThroughMembers_DoNotCount()
        {
            var provider = CreateProvider();
            provider.Set("a", 1, new CacheItemPolicy());
            provider.Reset();

            Assert.True(provider.Contains("a"));
            Assert.Equal(1, provider.GetCount());

            var counters = provider.Snapshot();
            Assert.Equal(0, counters.Reads);
        }

        [Fact]
        [DisplayName("併發讀取下計數不遺漏")]
        public void Get_UnderConcurrency_CountsEveryRead()
        {
            var provider = CreateProvider();
            provider.Set("a", 1, new CacheItemPolicy());
            provider.Reset();

            const int workers = 8;
            const int readsPerWorker = 500;
            Parallel.For(0, workers, _ =>
            {
                for (int i = 0; i < readsPerWorker; i++) { provider.Get("a"); }
            });

            Assert.Equal(workers * readsPerWorker, provider.Snapshot().Hits);
        }

        [Fact]
        [DisplayName("建構子拒絕 null 的內層 provider")]
        public void Constructor_NullInner_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CountingCacheProvider(null!));
        }
    }
}
