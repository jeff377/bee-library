using System.ComponentModel;
using Bee.ObjectCaching.Providers;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="CacheItemPolicy.ChangeNotifyKey"/> 的失效行為測試。
    /// </summary>
    /// <remarks>
    /// 每個測試都用 GUID 產生獨一無二的 notify key，因此即使共用 process-wide 的
    /// <see cref="CacheInfo.NotifyVersions"/>，也不會與其他平行測試互相干擾，
    /// 無需序列化或替換靜態狀態。
    /// </remarks>
    public class CacheNotifyTokenTests
    {
        private static string NewNotifyKey() => $"TestGroup:{Guid.NewGuid():N}";

        private static string NewCacheKey() => $"notify-{Guid.NewGuid():N}";

        [Fact]
        [DisplayName("notify 版本遞增後，帶 ChangeNotifyKey 的項目應失效")]
        public void Get_AfterVersionBump_EntryIsInvalidated()
        {
            using var provider = new MemoryCacheProvider();
            string notifyKey = NewNotifyKey();
            string cacheKey = NewCacheKey();

            provider.Set(cacheKey, "v1", new CacheItemPolicy { ChangeNotifyKey = notifyKey });
            Assert.Equal("v1", provider.Get(cacheKey));

            // 模擬另一個行程寫入定義後，poller 觀察到版本遞增。
            CacheInfo.NotifyVersions.SetVersion(notifyKey, 1);

            Assert.Null(provider.Get(cacheKey));
        }

        [Fact]
        [DisplayName("notify 版本未變動時，項目應保留")]
        public void Get_WithoutVersionBump_EntryIsRetained()
        {
            using var provider = new MemoryCacheProvider();
            string cacheKey = NewCacheKey();

            provider.Set(cacheKey, "v1", new CacheItemPolicy { ChangeNotifyKey = NewNotifyKey() });

            Assert.Equal("v1", provider.Get(cacheKey));
        }

        [Fact]
        [DisplayName("其他 notify key 的版本遞增不應影響無關項目")]
        public void Get_AfterUnrelatedVersionBump_EntryIsRetained()
        {
            using var provider = new MemoryCacheProvider();
            string cacheKey = NewCacheKey();

            provider.Set(cacheKey, "v1", new CacheItemPolicy { ChangeNotifyKey = NewNotifyKey() });
            CacheInfo.NotifyVersions.SetVersion(NewNotifyKey(), 99);

            Assert.Equal("v1", provider.Get(cacheKey));
        }

        [Fact]
        [DisplayName("未設定 ChangeNotifyKey 的項目不受任何版本遞增影響")]
        public void Get_WithoutNotifyKey_IgnoresVersionBump()
        {
            using var provider = new MemoryCacheProvider();
            string notifyKey = NewNotifyKey();
            string cacheKey = NewCacheKey();

            provider.Set(cacheKey, "v1", new CacheItemPolicy(CacheTimeKind.SlidingTime, 5));
            CacheInfo.NotifyVersions.SetVersion(notifyKey, 1);

            Assert.Equal("v1", provider.Get(cacheKey));
        }

        [Fact]
        [DisplayName("版本存放區未觀察過的 key 應回傳 0")]
        public void GetVersion_UnobservedKey_ReturnsZero()
        {
            var store = new CacheNotifyVersionStore();

            Assert.Equal(0L, store.GetVersion(NewNotifyKey()));
        }

        [Fact]
        [DisplayName("版本存放區應保留最後寫入的版本")]
        public void SetVersion_ThenGetVersion_ReturnsLatest()
        {
            var store = new CacheNotifyVersionStore();
            string notifyKey = NewNotifyKey();

            store.SetVersion(notifyKey, 3);
            store.SetVersion(notifyKey, 7);

            Assert.Equal(7L, store.GetVersion(notifyKey));
        }
    }
}
