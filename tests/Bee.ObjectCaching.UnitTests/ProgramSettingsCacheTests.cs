using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
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

                // 經 FileDefineStorage 走檔案後端;唯一 prefix 避免與其他測試共用快取鍵。
                var storage = new FileDefineStorage(pathOptions);
                string cachePrefix = Guid.NewGuid().ToString("N");
                var cache = new ProgramSettingsCache(storage, pathOptions, cachePrefix);

                // Get() → CreateInstance()（storage.GetProgramSettings 讀檔） → GetPolicy()（設定快取原則）
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
        [DisplayName("建構子傳入 null storage 應拋出 ArgumentNullException")]
        public void Constructor_NullStorage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgramSettingsCache(null!, new PathOptions()));
        }

        [Fact]
        [DisplayName("建構子傳入 null PathOptions 應拋出 ArgumentNullException")]
        public void Constructor_NullPathOptions_ThrowsArgumentNullException()
        {
            var storage = new FileDefineStorage(new PathOptions());
            Assert.Throws<ArgumentNullException>(() => new ProgramSettingsCache(storage, null!));
        }
    }
}
