using System.ComponentModel;
using Bee.UI.Avalonia.Storage;

namespace Bee.UI.Avalonia.UnitTests.Storage
{
    /// <summary>
    /// Verifies <see cref="FileEndpointStorage"/>'s caching contract: the constructor
    /// resolves the per-user file path, <see cref="FileEndpointStorage.SetEndpoint"/>
    /// mutates the in-memory cache only, and <see cref="FileEndpointStorage.SaveEndpoint"/>
    /// is the single method that touches the disk.
    /// </summary>
    public class FileEndpointStorageTests
    {
        // Each test uses a unique app name so parallel runs never collide, and the
        // created folder under LocalApplicationData is removed in a finally block.
        private static string NewAppName() => $"bee-avalonia-tests-{Guid.NewGuid():N}";

        private static string AppDirectory(string appName) => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName);

        private static void Cleanup(string appName)
        {
            try
            {
                Directory.Delete(AppDirectory(appName), recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
                // The test never wrote to disk; nothing to clean up.
            }
        }

        [Fact]
        [DisplayName("建構子在 appName 為 null 或空白時拋出例外")]
        public void Constructor_NullOrWhitespaceAppName_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FileEndpointStorage(null!));
            Assert.Throws<ArgumentException>(() => new FileEndpointStorage("   "));
        }

        [Fact]
        [DisplayName("FilePath 為 LocalApplicationData/<appName>/endpoint.txt")]
        public void FilePath_CombinesLocalAppDataAndAppName()
        {
            var appName = NewAppName();
            var storage = new FileEndpointStorage(appName);

            Assert.Equal(Path.Combine(AppDirectory(appName), "endpoint.txt"), storage.FilePath);
        }

        [Fact]
        [DisplayName("LoadEndpoint 在檔案不存在時回傳空字串")]
        public void LoadEndpoint_MissingFile_ReturnsEmpty()
        {
            var storage = new FileEndpointStorage(NewAppName());

            Assert.Equal(string.Empty, storage.LoadEndpoint());
        }

        [Fact]
        [DisplayName("SetEndpoint 只更新記憶體快取,不寫入磁碟")]
        public void SetEndpoint_CachesInMemoryWithoutTouchingDisk()
        {
            var appName = NewAppName();
            try
            {
                var storage = new FileEndpointStorage(appName);

                storage.SetEndpoint("https://api.example.com");

                Assert.Equal("https://api.example.com", storage.LoadEndpoint());
                Assert.False(File.Exists(storage.FilePath));
            }
            finally
            {
                Cleanup(appName);
            }
        }

        [Fact]
        [DisplayName("SaveEndpoint 建立目錄並寫入檔案,新實例可讀回")]
        public void SaveEndpoint_WritesFile_NewInstanceReadsItBack()
        {
            var appName = NewAppName();
            try
            {
                var storage = new FileEndpointStorage(appName);

                storage.SaveEndpoint("https://api.example.com/jsonrpc");

                Assert.True(File.Exists(storage.FilePath));
                var reloaded = new FileEndpointStorage(appName);
                Assert.Equal("https://api.example.com/jsonrpc", reloaded.LoadEndpoint());
            }
            finally
            {
                Cleanup(appName);
            }
        }

        [Fact]
        [DisplayName("LoadEndpoint 會修剪檔案內容前後空白")]
        public void LoadEndpoint_TrimsFileContent()
        {
            var appName = NewAppName();
            try
            {
                var storage = new FileEndpointStorage(appName);
                Directory.CreateDirectory(AppDirectory(appName));
                File.WriteAllText(storage.FilePath, "  https://api.example.com \n");

                Assert.Equal("https://api.example.com", storage.LoadEndpoint());
            }
            finally
            {
                Cleanup(appName);
            }
        }
    }
}
