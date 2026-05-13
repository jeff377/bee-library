using System.ComponentModel;
using Bee.ObjectCaching.Define;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    [Collection("CacheState")]
    public class DatabaseSettingsCacheTests : IClassFixture<SharedDbFixture>
    {
        public DatabaseSettingsCacheTests(SharedDbFixture _) { }


        [Fact]
        [DisplayName("CreateInstance 在 DatabaseSettings.xml 不存在時應拋出 FileNotFoundException")]
        public void CreateInstance_FileMissing_ThrowsFileNotFoundException()
        {
            var cache = new DatabaseSettingsCache();
            cache.Remove();

            using var temp = new TempDefinePath();

            Assert.Throws<FileNotFoundException>(() => cache.Get());
        }
    }
}
