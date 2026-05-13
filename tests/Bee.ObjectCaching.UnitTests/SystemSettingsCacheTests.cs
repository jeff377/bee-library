using System.ComponentModel;
using Bee.Definition;
using Bee.ObjectCaching.Define;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="SystemSettingsCache"/> 純邏輯測試 —— 構造 cache instance 時直接傳入指向
    /// 空目錄或共享 fixture path 的 <see cref="PathOptions"/>，不操弄 process-wide static。
    /// </summary>
    public class SystemSettingsCacheTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public SystemSettingsCacheTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("CreateInstance 在 SystemSettings.xml 不存在時應拋出 FileNotFoundException")]
        public void CreateInstance_FileMissing_ThrowsFileNotFoundException()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-syscache-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var paths = new PathOptions { DefinePath = tempDir };
                var cache = new SystemSettingsCache(paths, cachePrefix: $"sc_{Guid.NewGuid():N}");

                Assert.Throws<FileNotFoundException>(() => cache.Get());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("Get 在 SystemSettings.xml 存在時應回傳非空物件並觸發 GetPolicy")]
        public void Get_FileExists_ReturnsSettings()
        {
            // fixture 預設指向 tests/Define/，該目錄下存有 SystemSettings.xml；
            // 確保 GetPolicy（含 ChangeMonitorFilePaths 設定）被覆蓋。
            var cache = new SystemSettingsCache(_fx.PathOptions, cachePrefix: $"sc_{Guid.NewGuid():N}");

            var result = cache.Get();

            Assert.NotNull(result);
        }
    }
}
