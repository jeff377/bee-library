using System.ComponentModel;
using Bee.ObjectCaching.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;

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
        public void Set_WithAbsoluteExpiration_EvictsAfterDeadline()
        {
            // 假時鐘取代真實等待：過期與否由 MemoryCache 依 Clock 判定，推進時鐘即可，
            // 不需 sleep 到牆鐘真的走過期限（真實等待在負載高的 CI 上也未必可靠）。
            var clock = new FakeClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
            using var provider = new MemoryCacheProvider(
                new MemoryCache(new MemoryCacheOptions { Clock = clock }));
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = clock.UtcNow.AddMilliseconds(50)
            };
            provider.Set("k", "v", policy);
            Assert.Equal("v", provider.Get("k"));

            clock.Advance(TimeSpan.FromMilliseconds(51));

            Assert.Null(provider.Get("k"));
        }

        /// <summary>
        /// 可推進的假時鐘，供 <see cref="MemoryCacheOptions.Clock"/> 注入，
        /// 讓過期測試不依賴真實牆鐘。
        /// </summary>
        private sealed class FakeClock : ISystemClock
        {
            public FakeClock(DateTimeOffset start) { UtcNow = start; }

            public DateTimeOffset UtcNow { get; private set; }

            public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
        }

        [Fact]
        [DisplayName("Set 帶 ChangeMonitorFilePaths 應能成功建立快取")]
        public void Set_WithFileChangeMonitor_DoesNotThrow()
        {
            using var provider = CreateProvider();
            using var watchDir = WatchDirectory.Create();
            var tempFile = Path.Combine(watchDir.Path, "watched.tmp");
            File.WriteAllText(tempFile, "x");

            var policy = new CacheItemPolicy
            {
                ChangeMonitorFilePaths = new[] { tempFile },
                SlidingExpiration = TimeSpan.FromMinutes(1)
            };
            provider.Set("with-monitor", "v", policy);
            Assert.Equal("v", provider.Get("with-monitor"));
        }

        [Fact]
        [DisplayName("ChangeMonitorFilePaths 監控的檔案變更後快取項目應被驅逐")]
        public async Task Set_WithFileChangeMonitor_EvictsOnFileChange()
        {
            using var provider = CreateProvider();
            using var watchDir = WatchDirectory.Create();
            var tempFile = Path.Combine(watchDir.Path, "watched.tmp");
            File.WriteAllText(tempFile, "initial");

            var policy = new CacheItemPolicy
            {
                ChangeMonitorFilePaths = new[] { tempFile },
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };
            provider.Set("watched", "v", policy);
            Assert.True(provider.Contains("watched"));

            File.WriteAllText(tempFile, "changed");

            // FileModificationToken detects changes lazily on each Contains/Get call; poll until evicted.
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (provider.Contains("watched") && DateTime.UtcNow < deadline)
            {
                await Task.Delay(200);
            }

            Assert.False(provider.Contains("watched"));
        }

        /// <summary>
        /// 每測試獨立的暫存資料夾，包住要被監控的檔案。
        /// </summary>
        /// <remarks>
        /// <see cref="Microsoft.Extensions.FileProviders.PhysicalFileProvider"/> 對監控的檔案實際上是 watch
        /// 該檔案所在的父資料夾；若父資料夾被多個平行測試共用（如 <see cref="Path.GetTempPath"/>），
        /// 別的測試在那層建立或刪除其他檔案會誤觸發 change token，使本測試的快取項目被提前驅逐。
        /// 用獨立子資料夾隔離 watcher 即可避免此 race。
        /// </remarks>
        private sealed class WatchDirectory : IDisposable
        {
            public string Path { get; }

            private WatchDirectory(string path) { Path = path; }

            public static WatchDirectory Create()
            {
                var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bee-mctest-{Guid.NewGuid():N}");
                Directory.CreateDirectory(dir);
                return new WatchDirectory(dir);
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Path))
                        Directory.Delete(Path, recursive: true);
                }
                catch (IOException)
                {
                    // best effort
                }
            }
        }
    }
}
