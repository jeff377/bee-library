using System.ComponentModel;
using Bee.ObjectCaching.Providers;

namespace Bee.ObjectCaching.UnitTests
{
    public class MemoryCacheProviderTests
    {
        private static MemoryCacheProvider CreateProvider() => new();

        private static CacheItemPolicy DefaultPolicy() =>
            new CacheItemPolicy(CacheTimeKind.SlidingTime, 5);

        [Fact]
        [DisplayName("Set 後 Contains 應回傳 true，未存在的 key 應為 false")]
        public void Contains_AfterSet_ReturnsTrue_OtherwiseFalse()
        {
            using var provider = CreateProvider();
            provider.Set("foo", "bar", DefaultPolicy());

            Assert.True(provider.Contains("foo"));
            Assert.False(provider.Contains("missing"));
        }

        [Fact]
        [DisplayName("Get 應回傳先前 Set 的值")]
        public void Get_AfterSet_ReturnsValue()
        {
            using var provider = CreateProvider();
            provider.Set("hello", "world", DefaultPolicy());

            Assert.Equal("world", provider.Get("hello"));
        }

        [Fact]
        [DisplayName("Get 不存在的 key 應回傳 null")]
        public void Get_MissingKey_ReturnsNull()
        {
            using var provider = CreateProvider();
            Assert.Null(provider.Get("not-exists"));
        }

        [Fact]
        [DisplayName("Key 比對應為大小寫不敏感")]
        public void Set_KeyIsCaseInsensitive()
        {
            using var provider = CreateProvider();
            provider.Set("Mixed", 123, DefaultPolicy());

            Assert.True(provider.Contains("mixed"));
            Assert.True(provider.Contains("MIXED"));
            Assert.Equal(123, provider.Get("mIxEd"));
        }

        [Fact]
        [DisplayName("Remove 應移除指定快取項目")]
        public void Remove_ExistingKey_RemovesEntry()
        {
            using var provider = CreateProvider();
            provider.Set("k1", "v1", DefaultPolicy());

            provider.Remove("k1");

            Assert.False(provider.Contains("k1"));
        }

        [Fact]
        [DisplayName("Remove 不存在的 key 不應拋例外")]
        public void Remove_MissingKey_DoesNotThrow()
        {
            using var provider = CreateProvider();
            var exception = Record.Exception(() => provider.Remove("not-exists"));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("GetCount 應回應目前快取項目數量")]
        public void GetCount_ReflectsCurrentCache()
        {
            using var provider = CreateProvider();
            provider.Set("a", 1, DefaultPolicy());
            provider.Set("b", 2, DefaultPolicy());

            Assert.Equal(2, provider.GetCount());

            provider.Remove("a");
            Assert.Equal(1, provider.GetCount());
        }

        [Fact]
        [DisplayName("AbsoluteExpiration 過期後 Get 應回傳 null")]
        public async Task Set_WithAbsoluteExpiration_EvictsAfterDeadline()
        {
            using var provider = CreateProvider();
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMilliseconds(50)
            };
            provider.Set("k", "v", policy);

            await Task.Delay(200);

            Assert.Null(provider.Get("k"));
        }

        [Fact]
        [DisplayName("Set 帶 ChangeMonitorFilePaths 應能成功建立快取")]
        public void Set_WithFileChangeMonitor_DoesNotThrow()
        {
            using var provider = CreateProvider();
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

        [Fact]
        [DisplayName("ChangeMonitorFilePaths 監控的檔案變更後快取項目應被驅逐")]
        public async Task Set_WithFileChangeMonitor_EvictsOnFileChange()
        {
            using var provider = CreateProvider();
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(tempFile, "initial");
            try
            {
                var policy = new CacheItemPolicy
                {
                    ChangeMonitorFilePaths = new[] { tempFile },
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                };
                provider.Set("watched", "v", policy);
                Assert.True(provider.Contains("watched"));

                // Trigger a file change; PhysicalFileProvider polls every ~4 seconds by default.
                File.WriteAllText(tempFile, "changed");

                // PhysicalFileProvider polls every ~4 seconds in polling mode; allow generous slack on CI.
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (provider.Contains("watched") && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(200);
                }

                Assert.False(provider.Contains("watched"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
