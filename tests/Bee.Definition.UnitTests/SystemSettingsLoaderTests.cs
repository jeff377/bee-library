using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// SystemSettingsLoader 啟動期讀檔測試。
    /// 每個寫檔測試使用獨立的暫存目錄（透過 <see cref="TempDir"/>），不操弄
    /// <see cref="DefinePathInfo"/> 等 process-wide static，可與其他 test class 平行執行。
    /// </summary>
    public class SystemSettingsLoaderTests
    {
        [Fact]
        [DisplayName("Load(string) 給有效檔案路徑應回傳 SystemSettings 實例")]
        public void Load_ValidFile_ReturnsSettings()
        {
            using var temp = TempDir.Create();
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
            using var temp = TempDir.Create();
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
            using var temp = TempDir.Create();
            var paths = new PathOptions { DefinePath = temp.Path };
            var filePath = paths.GetSystemSettingsFilePath();
            var original = new SystemSettings();
            XmlCodec.SerializeToFile(original, filePath);

            var loaded = SystemSettingsLoader.Load(paths);

            Assert.NotNull(loaded);
            Assert.Equal(filePath, loaded.ObjectFilePath);
        }

        [Fact]
        [DisplayName("Load(PathOptions) 傳 null 應丟 ArgumentNullException")]
        public void Load_NullPathOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SystemSettingsLoader.Load((PathOptions)null!));
        }

        private sealed class TempDir : IDisposable
        {
            public string Path { get; }

            private TempDir(string path) { Path = path; }

            public static TempDir Create()
            {
                var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bee-loader-{Guid.NewGuid():N}");
                Directory.CreateDirectory(dir);
                return new TempDir(dir);
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Path))
                        Directory.Delete(Path, recursive: true);
                }
                catch (IOException)
                {
                    // best effort
                }
            }
        }
    }
}
