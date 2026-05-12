using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.UnitTests
{
    public class CacheInfoTests
    {
        [Fact]
        [DisplayName("CacheInfo.Initialize 傳入 null 設定應拋出 ArgumentNullException")]
        public void Initialize_NullConfiguration_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CacheInfo.Initialize(null!));
        }

        [Fact]
        [DisplayName("CacheInfo.Initialize CacheProvider 為空字串時應直接返回且不更換 Provider")]
        public void Initialize_EmptyCacheProvider_DoesNotChangeProvider()
        {
            var original = CacheInfo.Provider;
            var config = new BackendConfiguration();
            config.Components.CacheProvider = string.Empty;

            var exception = Record.Exception(() => CacheInfo.Initialize(config));

            Assert.Null(exception);
            Assert.Same(original, CacheInfo.Provider);
        }

        [Fact]
        [DisplayName("CacheInfo.Initialize 使用無法解析的 Provider 型別字串時應拋出例外")]
        public void Initialize_UnresolvableProviderType_ThrowsException()
        {
            // Type.GetType 回傳 null → 略過型別比對，直接進入 AssemblyLoader.CreateInstance，
            // 因組件不存在而拋出 FileNotFoundException，驗證 line 46 有被執行。
            var config = new BackendConfiguration();
            config.Components.CacheProvider = "Invalid.Type, Invalid.Assembly";

            Assert.ThrowsAny<Exception>(() => CacheInfo.Initialize(config));
        }
    }
}
