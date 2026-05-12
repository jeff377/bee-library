using System.ComponentModel;
using Bee.ObjectCaching.Define;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    [Collection("Initialize")]
    public class SystemSettingsCacheTests
    {
        [Fact]
        [DisplayName("CreateInstance 在 SystemSettings.xml 不存在時應拋出 FileNotFoundException")]
        public void CreateInstance_FileMissing_ThrowsFileNotFoundException()
        {
            var cache = new SystemSettingsCache();
            cache.Remove();

            using var temp = new TempDefinePath();

            Assert.Throws<FileNotFoundException>(() => cache.Get());
        }

        [Fact]
        [DisplayName("Get 在 SystemSettings.xml 存在時應回傳非空物件並觸發 GetPolicy")]
        public void Get_FileExists_ReturnsSettings()
        {
            // GlobalFixture 已將 BackendInfo.DefinePath 指向 tests/Define/，
            // 該目錄下存有 SystemSettings.xml，確保 GetPolicy（含 ChangeMonitorFilePaths 設定）被覆蓋。
            var cache = new SystemSettingsCache();
            cache.Remove();

            var result = cache.Get();

            Assert.NotNull(result);

            cache.Remove();
        }
    }
}
