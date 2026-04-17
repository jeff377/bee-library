using System.ComponentModel;
using System.IO;
using Bee.Base.Security;
using Bee.Definition.Security;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Security
{
    /// <summary>
    /// MasterKeyProvider 來源載入與錯誤路徑測試。
    /// </summary>
    public class MasterKeyProviderTests
    {
        [Fact]
        [DisplayName("GetMasterKey 檔案來源存在有效 Base64 應回傳對應位元組")]
        public void GetMasterKey_FileSource_ReturnsBytes()
        {
            // Arrange
            byte[] expected = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            string filePath = Path.Combine(Path.GetTempPath(), $"bee-mk-{Guid.NewGuid()}.key");
            File.WriteAllText(filePath, Convert.ToBase64String(expected));

            try
            {
                // Act
                byte[] actual = MasterKeyProvider.GetMasterKey(new MasterKeySource
                {
                    Type = MasterKeySourceType.File,
                    Value = filePath
                });

                // Assert
                Assert.Equal(expected, actual);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        [DisplayName("GetMasterKey 檔案不存在且 autoCreate=false 應拋出 FileNotFoundException")]
        public void GetMasterKey_FileMissing_NoAutoCreate_ThrowsFileNotFound()
        {
            // Arrange
            string missing = Path.Combine(Path.GetTempPath(), $"bee-mk-missing-{Guid.NewGuid()}.key");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
                MasterKeyProvider.GetMasterKey(new MasterKeySource
                {
                    Type = MasterKeySourceType.File,
                    Value = missing
                }));
        }

        [Fact]
        [DisplayName("GetMasterKey 檔案不存在且 autoCreate=true 應建立檔案並回傳內容")]
        public void GetMasterKey_FileMissing_AutoCreate_CreatesAndReturnsKey()
        {
            // Arrange
            string filePath = Path.Combine(Path.GetTempPath(), $"bee-mk-auto-{Guid.NewGuid()}.key");

            try
            {
                // Act
                byte[] result = MasterKeyProvider.GetMasterKey(
                    new MasterKeySource { Type = MasterKeySourceType.File, Value = filePath },
                    autoCreate: true);

                // Assert
                Assert.NotNull(result);
                Assert.NotEmpty(result);
                Assert.True(File.Exists(filePath));
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        [DisplayName("GetMasterKey 檔案內容非 Base64 應拋出 InvalidOperationException")]
        public void GetMasterKey_InvalidBase64Content_ThrowsInvalidOperation()
        {
            // Arrange
            string filePath = Path.Combine(Path.GetTempPath(), $"bee-mk-bad-{Guid.NewGuid()}.key");
            File.WriteAllText(filePath, "@@not-base64@@");

            try
            {
                // Act & Assert
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    MasterKeyProvider.GetMasterKey(new MasterKeySource
                    {
                        Type = MasterKeySourceType.File,
                        Value = filePath
                    }));
                Assert.IsType<FormatException>(ex.InnerException);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        [DisplayName("GetMasterKey 檔案內容為空應拋出 InvalidOperationException")]
        public void GetMasterKey_EmptyFileContent_ThrowsInvalidOperation()
        {
            // Arrange
            string filePath = Path.Combine(Path.GetTempPath(), $"bee-mk-empty-{Guid.NewGuid()}.key");
            File.WriteAllText(filePath, "   ");

            try
            {
                // Act & Assert
                Assert.Throws<InvalidOperationException>(() =>
                    MasterKeyProvider.GetMasterKey(new MasterKeySource
                    {
                        Type = MasterKeySourceType.File,
                        Value = filePath
                    }));
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        [DisplayName("GetMasterKey 環境變數來源存在應回傳對應位元組")]
        public void GetMasterKey_EnvironmentSource_ReturnsBytes()
        {
            // Arrange
            string varName = $"BEE_TEST_MK_{Guid.NewGuid():N}";
            byte[] expected = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            Environment.SetEnvironmentVariable(varName, Convert.ToBase64String(expected));

            try
            {
                // Act
                byte[] actual = MasterKeyProvider.GetMasterKey(new MasterKeySource
                {
                    Type = MasterKeySourceType.Environment,
                    Value = varName
                });

                // Assert
                Assert.Equal(expected, actual);
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, null);
            }
        }

        [Fact]
        [DisplayName("GetMasterKey 環境變數不存在且 autoCreate=false 應拋出 InvalidOperationException")]
        public void GetMasterKey_EnvironmentMissing_NoAutoCreate_Throws()
        {
            // Arrange
            string varName = $"BEE_TEST_MK_MISSING_{Guid.NewGuid():N}";
            Environment.SetEnvironmentVariable(varName, null);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                MasterKeyProvider.GetMasterKey(new MasterKeySource
                {
                    Type = MasterKeySourceType.Environment,
                    Value = varName
                }));
        }

        [Fact]
        [DisplayName("GetMasterKey 環境變數不存在且 autoCreate=true 應建立變數並回傳內容")]
        public void GetMasterKey_EnvironmentMissing_AutoCreate_CreatesAndReturnsKey()
        {
            // Arrange
            string varName = $"BEE_TEST_MK_AUTO_{Guid.NewGuid():N}";
            Environment.SetEnvironmentVariable(varName, null);

            try
            {
                // Act
                byte[] result = MasterKeyProvider.GetMasterKey(
                    new MasterKeySource { Type = MasterKeySourceType.Environment, Value = varName },
                    autoCreate: true);

                // Assert
                Assert.NotNull(result);
                Assert.NotEmpty(result);
                Assert.False(string.IsNullOrEmpty(Environment.GetEnvironmentVariable(varName)));
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, null);
            }
        }
    }
}
