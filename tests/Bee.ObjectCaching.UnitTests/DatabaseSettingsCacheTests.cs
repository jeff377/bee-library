using System.ComponentModel;
using Bee.Definition;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="DatabaseSettingsCache"/> 純邏輯測試 —— 構造 cache instance 時直接傳入指向
    /// 空目錄的 <see cref="PathOptions"/>，不操弄 process-wide static，可與其他 test class 平行執行。
    /// </summary>
    public class DatabaseSettingsCacheTests
    {
        [Fact]
        [DisplayName("CreateInstance 在 DatabaseSettings.xml 不存在時應拋出 FileNotFoundException")]
        public void CreateInstance_FileMissing_ThrowsFileNotFoundException()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-dbcache-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var paths = new PathOptions { DefinePath = tempDir };
                // Per-test cache prefix 確保 instance 不會與其他 fixture 的 cache 互相干擾。
                var cache = new DatabaseSettingsCache(paths, cachePrefix: $"dbc_{Guid.NewGuid():N}");

                Assert.Throws<FileNotFoundException>(() => cache.Get());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }
    }
}
