using System.ComponentModel;
using Bee.Definition.Settings;

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
    }
}
