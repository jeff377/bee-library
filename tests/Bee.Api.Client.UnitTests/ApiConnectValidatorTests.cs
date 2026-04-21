using System.ComponentModel;
using Bee.Tests.Shared;

namespace Bee.Api.Client.UnitTests
{
    [Collection("Initialize")]
    public class ApiConnectValidatorTests
    {
        [LocalOnlyTheory]
        [DisplayName("ApiConnectValidator 驗證 URL 應回傳遠端連線類型")]
        [InlineData("http://localhost/jsonrpc/api")]
        //[InlineData("http://localhost/jsonrpc_aspnet/api")]
        public void Validate_ValidUrl_ReturnsRemoteConnectType(string apiUrl)
        {
            var connectType = ApiConnectValidator.Validate(apiUrl);

            Assert.Equal(ConnectType.Remote, connectType);  // 確認連線方式為遠端連線
        }

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
            var original = ApiClientContext.SupportedConnectTypes;
            try
            {
                ApiClientContext.SupportedConnectTypes = SupportedConnectTypes.Remote;
                Assert.Throws<InvalidOperationException>(() => ApiConnectValidator.Validate(@"C:\FakePath_NoLocalSupport"));
            }
            finally
            {
                ApiClientContext.SupportedConnectTypes = original;
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.Validate 本機路徑不存在時應拋 ArgumentException")]
        public void Validate_LocalPath_NotExists_ThrowsArgumentException()
        {
            var original = ApiClientContext.SupportedConnectTypes;
            try
            {
                ApiClientContext.SupportedConnectTypes = SupportedConnectTypes.Both;
                Assert.Throws<ArgumentException>(() => ApiConnectValidator.Validate(@"C:\NonExistent_bee_test_abc123"));
            }
            finally
            {
                ApiClientContext.SupportedConnectTypes = original;
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
            var originalSupported = ApiClientContext.SupportedConnectTypes;
            try
            {
                ApiClientContext.SupportedConnectTypes = SupportedConnectTypes.Both;
                Assert.Throws<FileNotFoundException>(() => ApiConnectValidator.Validate(tempDir));
            }
            finally
            {
                ApiClientContext.SupportedConnectTypes = originalSupported;
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
            var originalSupported = ApiClientContext.SupportedConnectTypes;
            try
            {
                ApiClientContext.SupportedConnectTypes = SupportedConnectTypes.Both;
                var result = ApiConnectValidator.Validate(tempDir, allowGenerateSettings: true);

                Assert.Equal(ConnectType.Local, result);
                Assert.True(File.Exists(Path.Combine(tempDir, "SystemSettings.xml")));
                Assert.True(File.Exists(Path.Combine(tempDir, "DatabaseSettings.xml")));
            }
            finally
            {
                ApiClientContext.SupportedConnectTypes = originalSupported;
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.Validate URL 但不支援遠端連線時應拋 InvalidOperationException")]
        public void Validate_Url_RemoteNotSupported_ThrowsInvalidOperationException()
        {
            var original = ApiClientContext.SupportedConnectTypes;
            try
            {
                ApiClientContext.SupportedConnectTypes = SupportedConnectTypes.Local;
                Assert.Throws<InvalidOperationException>(() => ApiConnectValidator.Validate("http://example.com/api"));
            }
            finally
            {
                ApiClientContext.SupportedConnectTypes = original;
            }
        }
    }
}
