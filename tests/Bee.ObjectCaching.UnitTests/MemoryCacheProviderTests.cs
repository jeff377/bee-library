using System.ComponentModel;
using System.Runtime.Caching;
using Bee.ObjectCaching.Providers;

namespace Bee.ObjectCaching.UnitTests
{
    public class MemoryCacheProviderTests
    {
        private static MemoryCacheProvider CreateProvider(out MemoryCache memoryCache)
        {
            // 每個測試使用獨立的 MemoryCache 實例，避免共享 MemoryCache.Default 造成測試間污染
            memoryCache = new MemoryCache("Bee.ObjectCaching.Tests." + Guid.NewGuid().ToString("N"));
            return new MemoryCacheProvider(memoryCache);
        }

        private static CacheItemPolicy DefaultPolicy() =>
            new CacheItemPolicy(CacheTimeKind.SlidingTime, 5);

        [Fact]
        [DisplayName("Set 後 Contains 應回傳 true，未存在的 key 應為 false")]
        public void Contains_AfterSet_ReturnsTrue_OtherwiseFalse()
        {
            var provider = CreateProvider(out _);
            provider.Set("foo", "bar", DefaultPolicy());

            Assert.True(provider.Contains("foo"));
            Assert.False(provider.Contains("missing"));
        }

        [Fact]
        [DisplayName("Get 應回傳先前 Set 的值")]
        public void Get_AfterSet_ReturnsValue()
        {
            var provider = CreateProvider(out _);
            provider.Set("hello", "world", DefaultPolicy());

            Assert.Equal("world", provider.Get("hello"));
        }

        [Fact]
        [DisplayName("Key 比對應為大小寫不敏感")]
        public void Set_KeyIsCaseInsensitive()
        {
            var provider = CreateProvider(out _);
            provider.Set("Mixed", 123, DefaultPolicy());

            Assert.True(provider.Contains("mixed"));
            Assert.True(provider.Contains("MIXED"));
            Assert.Equal(123, provider.Get("mIxEd"));
        }

        [Fact]
        [DisplayName("Remove 應移除指定快取項目並回傳被移除的值")]
        public void Remove_ExistingKey_RemovesAndReturnsValue()
        {
            var provider = CreateProvider(out _);
            provider.Set("k1", "v1", DefaultPolicy());

            var removed = provider.Remove("k1");

            Assert.Equal("v1", removed);
            Assert.False(provider.Contains("k1"));
        }

        [Fact]
        [DisplayName("Remove 不存在的 key 應回傳 null")]
        public void Remove_MissingKey_ReturnsNull()
        {
            var provider = CreateProvider(out _);
            Assert.Null(provider.Remove("not-exists"));
        }

        [Fact]
        [DisplayName("GetCount 與 GetAllKeys 應回應目前快取內容")]
        public void GetCount_GetAllKeys_ReflectCurrentCache()
        {
            var provider = CreateProvider(out _);
            provider.Set("a", 1, DefaultPolicy());
            provider.Set("b", 2, DefaultPolicy());

            Assert.Equal(2, provider.GetCount());
            var keys = provider.GetAllKeys().ToList();
            Assert.Equal(2, keys.Count);
            // 內部會將 key 轉成大寫存放
            Assert.Contains("A", keys);
            Assert.Contains("B", keys);
        }

        [Fact]
        [DisplayName("Trim(100) 應移除全部快取項目")]
        public void Trim_All_RemovesEverything()
        {
            var provider = CreateProvider(out _);
            provider.Set("a", 1, DefaultPolicy());
            provider.Set("b", 2, DefaultPolicy());

            provider.Trim(100);

            // Trim 採近似演算法，此處只驗證最終狀態
            Assert.Equal(0, provider.GetCount());
        }

        [Fact]
        [DisplayName("使用無參數建構子應對應到 MemoryCache.Default")]
        public void DefaultConstructor_DoesNotThrow()
        {
            var provider = new MemoryCacheProvider();
            // 寫入後立刻清除，避免影響其他使用 MemoryCache.Default 的測試
            var key = "MemoryCacheProviderTests_default_" + Guid.NewGuid().ToString("N");
            try
            {
                provider.Set(key, "v", DefaultPolicy());
                Assert.True(provider.Contains(key));
            }
            finally
            {
                provider.Remove(key);
            }
        }

        [Fact]
        [DisplayName("Set 帶 ChangeMonitorFilePaths 的 policy 應能成功建立快取")]
        public void Set_WithFileChangeMonitor_DoesNotThrow()
        {
            var provider = CreateProvider(out _);
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(tempFile, "x");
            try
            {
                var policy = new CacheItemPolicy
                {
                    ChangeMonitorFilePaths = new[] { tempFile },
                    SlidingExpiration = TimeSpan.FromMinutes(1)
                };
                provider.Set("with-monitor", "v", policy);
                Assert.Equal("v", provider.Get("with-monitor"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
