using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// SystemSettingsLoader 啟動期讀檔測試。
    /// 與 <see cref="BackendInfo.DefinePath"/> 互動的測試使用 <see cref="TempDefinePath"/> 切到隔離目錄，
    /// 避免動到 tests/Define/SystemSettings.xml 共享 fixture；同時加入 Initialize collection 以避免
    /// 與其他操弄全域狀態的測試並行執行。
    /// </summary>
    [Collection("Initialize")]
    public class SystemSettingsLoaderTests
    {
        [Fact]
        [DisplayName("Load 給有效檔案路徑應回傳 SystemSettings 實例")]
        public void Load_ValidFile_ReturnsSettings()
        {
            using var temp = new TempDefinePath();
            var filePath = Path.Combine(temp.Path, "SystemSettings.xml");
            var original = new SystemSettings();
            XmlCodec.SerializeToFile(original, filePath);

            var loaded = SystemSettingsLoader.Load(filePath);

            Assert.NotNull(loaded);
            Assert.NotNull(loaded.BackendConfiguration);
            Assert.NotNull(loaded.CommonConfiguration);
        }

        [Fact]
        [DisplayName("Load 對不存在的檔案路徑應丟 FileNotFoundException")]
        public void Load_FileNotFound_ThrowsFileNotFoundException()
        {
            using var temp = new TempDefinePath();
            var missingPath = Path.Combine(temp.Path, "Nope.xml");

            Assert.Throws<FileNotFoundException>(() => SystemSettingsLoader.Load(missingPath));
        }

        [Fact]
        [DisplayName("Load 傳 null 路徑應丟 ArgumentException")]
        public void Load_NullPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => SystemSettingsLoader.Load(null!));
        }

        [Fact]
        [DisplayName("Load 傳空白路徑應丟 ArgumentException")]
        public void Load_WhitespacePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => SystemSettingsLoader.Load("   "));
        }

        [Fact]
        [DisplayName("Load 無參數版本應透過 BackendInfo.DefinePath 解析 SystemSettings.xml")]
        public void Load_NoArgs_UsesDefinePathInfo()
        {
            using var temp = new TempDefinePath();
            var filePath = DefinePathInfo.GetSystemSettingsFilePath();
            var original = new SystemSettings();
            XmlCodec.SerializeToFile(original, filePath);

            var loaded = SystemSettingsLoader.Load();

            Assert.NotNull(loaded);
            Assert.Equal(filePath, loaded.ObjectFilePath);
        }
    }
}
