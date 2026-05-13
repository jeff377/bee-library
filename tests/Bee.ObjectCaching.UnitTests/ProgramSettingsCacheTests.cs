using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching.UnitTests
{
    public class ProgramSettingsCacheTests
    {
        [Fact]
        [DisplayName("Get 於檔案存在時應反序列化並回傳 ProgramSettings（同時覆蓋 GetPolicy 路徑）")]
        public void Get_FileExists_ReturnsProgramSettings()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-psc-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var pathOptions = new PathOptions { DefinePath = tempDir };
                XmlCodec.SerializeToFile(new ProgramSettings(), pathOptions.GetProgramSettingsFilePath());

                // 使用唯一 prefix 避免與其他測試共用快取鍵
                string cachePrefix = Guid.NewGuid().ToString("N");
                var cache = new ProgramSettingsCache(pathOptions, cachePrefix);

                // Get() → CreateInstance()（讀檔反序列化） → GetPolicy()（設定快取原則）
                var result = cache.Get();

                Assert.NotNull(result);
                cache.Remove();
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("建構子傳入 null PathOptions 應拋出 ArgumentNullException")]
        public void Constructor_NullPathOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgramSettingsCache(null!));
        }
    }
}
