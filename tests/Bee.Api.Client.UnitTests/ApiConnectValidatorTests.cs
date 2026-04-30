using System.ComponentModel;

namespace Bee.Api.Client.UnitTests
{
    [Collection("Initialize")]
    public class ApiConnectValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("ApiConnectValidator.Validate 空白 endpoint 應拋 ArgumentException")]
        public void Validate_EmptyEndpoint_ThrowsArgumentException(string? endpoint)
        {
            Assert.Throws<ArgumentException>(() => ApiConnectValidator.Validate(endpoint!));
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("not-a-url")]
        [InlineData("ftp://example.com")]
        [DisplayName("ApiConnectValidator.Validate 無法辨識的格式應拋 InvalidOperationException")]
        public void Validate_UnknownFormat_ThrowsInvalidOperationException(string endpoint)
        {
            Assert.Throws<InvalidOperationException>(() => ApiConnectValidator.Validate(endpoint));
        }

        [Fact]
        [DisplayName("ApiConnectValidator.Validate 本機路徑但不支援 Local 時應拋 InvalidOperationException")]
        public void Validate_LocalPath_NotSupported_ThrowsInvalidOperationException()
        {
            var original = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
                Assert.Throws<InvalidOperationException>(() => ApiConnectValidator.Validate(@"C:\FakePath_NoLocalSupport"));
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = original;
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.Validate 本機路徑不存在時應拋 ArgumentException")]
        public void Validate_LocalPath_NotExists_ThrowsArgumentException()
        {
            var original = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                Assert.Throws<ArgumentException>(() => ApiConnectValidator.Validate(@"C:\NonExistent_bee_test_abc123"));
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = original;
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.Validate 本機路徑存在但缺 SystemSettings.xml 應拋 FileNotFoundException")]
        public void Validate_LocalPath_MissingSystemSettings_ThrowsFileNotFoundException()
        {
            // 此測試倚賴 Windows 形式的路徑（drive:\）與實體檔案系統，CI Linux 環境下跳過。
            if (!OperatingSystem.IsWindows()) return;

            var tempDir = Path.Combine(Path.GetTempPath(), "bee_api_client_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var originalSupported = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                Assert.Throws<FileNotFoundException>(() => ApiConnectValidator.Validate(tempDir));
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = originalSupported;
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.Validate 啟用 allowGenerateSettings 時應建立 SystemSettings.xml 與 DatabaseSettings.xml")]
        public void Validate_LocalPath_AllowGenerateSettings_CreatesFiles()
        {
            // 此測試倚賴 Windows 形式的路徑與實體檔案系統寫入。
            if (!OperatingSystem.IsWindows()) return;

            var tempDir = Path.Combine(Path.GetTempPath(), "bee_api_client_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var originalSupported = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                var result = ApiConnectValidator.Validate(tempDir, allowGenerateSettings: true);

                Assert.Equal(ConnectType.Local, result);
                Assert.True(File.Exists(Path.Combine(tempDir, "SystemSettings.xml")));
                Assert.True(File.Exists(Path.Combine(tempDir, "DatabaseSettings.xml")));
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = originalSupported;
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.Validate URL 格式但不支援 Remote 時應拋 InvalidOperationException")]
        public void Validate_RemoteUrl_RemoteNotSupported_ThrowsInvalidOperationException()
        {
            var original = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Local;
                Assert.Throws<InvalidOperationException>(
                    () => ApiConnectValidator.Validate("http://example.com/api"));
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = original;
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.Validate URL 無法連線時應拋 InvalidOperationException 並指出 endpoint not reachable")]
        public void Validate_RemoteUrl_NotReachable_ThrowsEndpointNotReachable()
        {
            var original = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                // 127.0.0.1:1 為 reserved port，本機不會有服務監聽，預檢必然失敗
                var ex = Assert.Throws<InvalidOperationException>(
                    () => ApiConnectValidator.Validate("http://127.0.0.1:1/jsonrpc/api"));
                Assert.Contains("Endpoint not reachable", ex.Message);
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = original;
            }
        }
    }
}
