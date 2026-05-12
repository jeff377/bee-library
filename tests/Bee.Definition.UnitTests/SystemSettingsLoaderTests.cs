using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// SystemSettingsLoader 啟動期讀檔測試。
    /// 與 DefinePath 互動的測試使用 <see cref="TempDefinePath"/> 切到隔離目錄，
    /// 避免動到 tests/Define/SystemSettings.xml 共享 fixture；同時加入 Initialize collection 以避免
    /// 與其他操弄全域狀態的測試並行執行。
    /// </summary>
    [Collection("Initialize")]
    public class SystemSettingsLoaderTests
    {
        [Fact]
        [DisplayName("Load(string) 給有效檔案路徑應回傳 SystemSettings 實例")]
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
        [DisplayName("Load(string) 對不存在的檔案路徑應丟 FileNotFoundException")]
        public void Load_FileNotFound_ThrowsFileNotFoundException()
        {
            using var temp = new TempDefinePath();
            var missingPath = Path.Combine(temp.Path, "Nope.xml");

            Assert.Throws<FileNotFoundException>(() => SystemSettingsLoader.Load(missingPath));
        }

        [Fact]
        [DisplayName("Load(string) 傳 null 路徑應丟 ArgumentNullException")]
        public void Load_NullPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SystemSettingsLoader.Load((string)null!));
        }

        [Fact]
        [DisplayName("Load(string) 傳空白路徑應丟 ArgumentException")]
        public void Load_WhitespacePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => SystemSettingsLoader.Load("   "));
        }

        [Fact]
        [DisplayName("Load(PathOptions) 應透過 PathOptions 解析 SystemSettings.xml")]
        public void Load_WithPathOptions_ResolvesViaPathOptions()
        {
            using var temp = new TempDefinePath();
            var filePath = temp.Options.GetSystemSettingsFilePath();
            var original = new SystemSettings();
            XmlCodec.SerializeToFile(original, filePath);

            var loaded = SystemSettingsLoader.Load(temp.Options);

            Assert.NotNull(loaded);
            Assert.Equal(filePath, loaded.ObjectFilePath);
        }

        [Fact]
        [DisplayName("Load(PathOptions) 傳 null 應丟 ArgumentNullException")]
        public void Load_NullPathOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SystemSettingsLoader.Load((PathOptions)null!));
        }
    }
}
