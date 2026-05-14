using System.ComponentModel;
using Bee.Definition.Settings;
using Bee.ObjectCaching.Providers;

namespace Bee.ObjectCaching.UnitTests
{
    public class CacheInfoTests
    {
        [Fact]
        [DisplayName("Initialize 設定型別與現有 Provider 相同時應保留現有 Provider 實例（覆蓋型別比對路徑）")]
        public void Initialize_SameProviderType_DoesNotReplaceProvider()
        {
            var configuration = new BackendConfiguration();
            configuration.Components.CacheProvider =
                "Bee.ObjectCaching.Providers.MemoryCacheProvider, Bee.ObjectCaching";
            var originalProvider = CacheInfo.Provider;

            CacheInfo.Initialize(configuration);

            // Provider 應保持同一實例（型別相同 → 提前返回）
            Assert.Same(originalProvider, CacheInfo.Provider);
        }

        [Fact]
        [DisplayName("Initialize 傳入 null 應拋出 ArgumentNullException")]
        public void Initialize_NullConfiguration_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CacheInfo.Initialize(null!));
        }

        [Fact]
        [DisplayName("Initialize CacheProvider 為空字串時應提前返回且不替換 Provider")]
        public void Initialize_EmptyCacheProvider_ReturnsEarlyWithoutChange()
        {
            var config = new BackendConfiguration();
            config.Components.CacheProvider = string.Empty;
            var originalProvider = CacheInfo.Provider;

            CacheInfo.Initialize(config);

            Assert.Same(originalProvider, CacheInfo.Provider);
        }

        [Fact]
        [DisplayName("Initialize 設定型別與現有 Provider 不同時應建立新 Provider 實例")]
        public void Initialize_DifferentProviderType_ReplacesProvider()
        {
            // 使用預設 MemoryCacheProvider 設定，但先把靜態 Provider 換成不同型別
            var config = new BackendConfiguration();
            var originalProvider = CacheInfo.Provider;
            CacheInfo.Provider = new FakeCacheProvider();
            try
            {
                CacheInfo.Initialize(config);
                // 型別不同 → 提前返回條件不成立 → 建立新的 MemoryCacheProvider
                Assert.IsType<MemoryCacheProvider>(CacheInfo.Provider);
            }
            finally
            {
                CacheInfo.Provider = originalProvider;
            }
        }

        private sealed class FakeCacheProvider : ICacheProvider
        {
            public bool Contains(string key) => false;
            public void Set(string key, object value, CacheItemPolicy policy) { }
            public object? Get(string key) => null;
            public void Remove(string key) { }
            public long GetCount() => 0;
        }
    }
}
