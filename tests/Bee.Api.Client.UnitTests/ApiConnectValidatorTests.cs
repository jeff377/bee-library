using System.ComponentModel;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// ApiConnectValidator 路徑驗證測試。每個用例以 try/finally 還原
    /// <see cref="ApiClientInfo.SupportedConnectTypes"/>；類別內測試串列執行（xUnit 預設），
    /// 與其他 test class 平行時的 race 風險：<c>ApiClientInfoTests</c> 也會 mutate 同一 static，
    /// 但兩者皆走 snapshot/restore；二者同列入 <c>[Collection("ApiClientInfoState")]</c> 串行，
    /// 避免平行 class 互改 static 造成的 race。
    /// </summary>
    [Collection("ApiClientInfoState")]
    public class ApiConnectValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("ApiConnectValidator.ValidateAsync 空白 endpoint 應拋 ArgumentException")]
        public async Task ValidateAsync_EmptyEndpoint_ThrowsArgumentException(string? endpoint)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => ApiConnectValidator.ValidateAsync(endpoint!));
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("not-a-url")]
        [InlineData("ftp://example.com")]
        [DisplayName("ApiConnectValidator.ValidateAsync 無法辨識的格式應拋 InvalidOperationException")]
        public async Task ValidateAsync_UnknownFormat_ThrowsInvalidOperationException(string endpoint)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => ApiConnectValidator.ValidateAsync(endpoint));
        }

        [Fact]
        [DisplayName("ApiConnectValidator.ValidateAsync 本機路徑但不支援 Local 時應拋 InvalidOperationException")]
        public async Task ValidateAsync_LocalPath_NotSupported_ThrowsInvalidOperationException()
        {
            var original = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => ApiConnectValidator.ValidateAsync(@"C:\FakePath_NoLocalSupport"));
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = original;
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.ValidateAsync 本機路徑不存在時應拋 ArgumentException")]
        public async Task ValidateAsync_LocalPath_NotExists_ThrowsArgumentException()
        {
            var original = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                await Assert.ThrowsAsync<ArgumentException>(
                    () => ApiConnectValidator.ValidateAsync(@"C:\NonExistent_bee_test_abc123"));
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = original;
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.ValidateAsync 本機路徑存在但缺 SystemSettings.xml 應拋 FileNotFoundException")]
        public async Task ValidateAsync_LocalPath_MissingSystemSettings_ThrowsFileNotFoundException()
        {
            // 此測試倚賴 Windows 形式的路徑（drive:\）與實體檔案系統，CI Linux 環境下跳過。
            if (!OperatingSystem.IsWindows()) return;

            var tempDir = Path.Combine(Path.GetTempPath(), "bee_api_client_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var originalSupported = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                await Assert.ThrowsAsync<FileNotFoundException>(
                    () => ApiConnectValidator.ValidateAsync(tempDir));
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = originalSupported;
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.ValidateAsync 啟用 allowGenerateSettings 時應建立 SystemSettings.xml 與 DatabaseSettings.xml")]
        public async Task ValidateAsync_LocalPath_AllowGenerateSettings_CreatesFiles()
        {
            // 此測試倚賴 Windows 形式的路徑與實體檔案系統寫入。
            if (!OperatingSystem.IsWindows()) return;

            var tempDir = Path.Combine(Path.GetTempPath(), "bee_api_client_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var originalSupported = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                var result = await ApiConnectValidator.ValidateAsync(tempDir, allowGenerateSettings: true);

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
        [DisplayName("ApiConnectValidator.ValidateAsync URL 格式但不支援 Remote 時應拋 InvalidOperationException")]
        public async Task ValidateAsync_RemoteUrl_RemoteNotSupported_ThrowsInvalidOperationException()
        {
            var original = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Local;
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => ApiConnectValidator.ValidateAsync("http://example.com/api"));
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = original;
            }
        }

        [Fact]
        [DisplayName("ApiConnectValidator.ValidateAsync URL 無法連線時應拋 InvalidOperationException 並指出 endpoint not reachable")]
        public async Task ValidateAsync_RemoteUrl_NotReachable_ThrowsEndpointNotReachable()
        {
            var original = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                // 127.0.0.1:1 為 reserved port，本機不會有服務監聽，預檢必然失敗
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => ApiConnectValidator.ValidateAsync("http://127.0.0.1:1/jsonrpc/api"));
                Assert.Contains("Endpoint not reachable", ex.Message);
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = original;
            }
        }
    }
}
