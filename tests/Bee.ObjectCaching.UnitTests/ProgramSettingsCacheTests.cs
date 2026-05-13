using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.ObjectCaching.Define;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    [Collection("Initialize")]
    public class ProgramSettingsCacheTests
    {
        [Fact]
        [DisplayName("CreateInstance 於 ProgramSettings.xml 存在時應回傳 ProgramSettings 並觸發 GetPolicy")]
        public void CreateInstance_FileExists_ReturnsProgramSettings()
        {
            var cache = new ProgramSettingsCache();
            cache.Remove();

            using var temp = new TempDefinePath();
            string filePath = DefinePathInfo.GetProgramSettingsFilePath();
            XmlCodec.SerializeToFile(new ProgramSettings(), filePath);

            try
            {
                var result = cache.Get();
                Assert.NotNull(result);
                Assert.IsType<ProgramSettings>(result);
            }
            finally
            {
                cache.Remove();
            }
        }
    }
}
