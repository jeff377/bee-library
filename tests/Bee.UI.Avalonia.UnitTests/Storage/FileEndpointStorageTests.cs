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
        [DisplayName("ApiKeyFilePath 為 LocalApplicationData/<appName>/apikey.txt")]
        public void ApiKeyFilePath_CombinesLocalAppDataAndAppName()
        {
            var appName = NewAppName();
            var storage = new FileEndpointStorage(appName);

            Assert.Equal(Path.Combine(AppDirectory(appName), "apikey.txt"), storage.ApiKeyFilePath);
        }

        [Fact]
        [DisplayName("SetApiKey 只改記憶體快取，不落地；SaveApiKey 才寫檔")]
        public void SetApiKey_DoesNotTouchDisk_SaveApiKeyDoes()
        {
            var appName = NewAppName();
            try
            {
                var storage = new FileEndpointStorage(appName);

                storage.SetApiKey("cached-only.secret");
                Assert.Equal("cached-only.secret", storage.LoadApiKey());
                Assert.False(File.Exists(storage.ApiKeyFilePath));

                storage.SaveApiKey("persisted.secret");
                Assert.True(File.Exists(storage.ApiKeyFilePath));
                Assert.Equal("persisted.secret", File.ReadAllText(storage.ApiKeyFilePath));
            }
            finally
            {
                Cleanup(appName);
            }
        }

        [Fact]
        [DisplayName("SaveApiKey 寫入後新實例應讀回同一把金鑰，且與 endpoint 互不干擾")]
        public void SaveApiKey_RoundTripsIndependentlyOfEndpoint()
        {
            var appName = NewAppName();
            try
            {
                var writer = new FileEndpointStorage(appName);
                writer.SaveEndpoint("http://host:5100/api");
                writer.SaveApiKey("app-id.secret");

                var reader = new FileEndpointStorage(appName);
                Assert.Equal("http://host:5100/api", reader.LoadEndpoint());
                Assert.Equal("app-id.secret", reader.LoadApiKey());
            }
            finally
            {
                Cleanup(appName);
            }
        }

        [Fact]
        [DisplayName("LoadApiKey 於尚未寫入時應回空字串")]
        public void LoadApiKey_NoFile_ReturnsEmpty()
        {
            var appName = NewAppName();
            try
            {
                Assert.Equal(string.Empty, new FileEndpointStorage(appName).LoadApiKey());
            }
            finally
            {
                Cleanup(appName);
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
