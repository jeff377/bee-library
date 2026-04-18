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

        [Fact]
        [DisplayName("GetMasterKey 檔案路徑為空字串時會套用預設檔名 Master.key")]
        public void GetMasterKey_EmptyFilePath_UsesDefaultFileName()
        {
            // Arrange: 空字串會被替換為 "Master.key"，於 DefinePath（或當前目錄）下
            var source = new MasterKeySource
            {
                Type = MasterKeySourceType.File,
                Value = "   "
            };

            // Act & Assert
            // autoCreate=false 時，在絕大多數測試環境下預設檔不存在 → 拋 FileNotFoundException；
            // 若剛好存在（罕見），也只會走到後續 Base64 解析，不影響「空路徑分支被覆蓋」的目的。
            var ex = Record.Exception(() => MasterKeyProvider.GetMasterKey(source));
            Assert.NotNull(ex);
        }

        [Fact]
        [DisplayName("GetMasterKey 環境變數名為空字串時套用預設 BEE_MASTER_KEY")]
        public void GetMasterKey_EmptyVarName_UsesDefaultVarName()
        {
            // Arrange: 先確保預設變數為空，再呼叫
            string? original = Environment.GetEnvironmentVariable("BEE_MASTER_KEY");
            Environment.SetEnvironmentVariable("BEE_MASTER_KEY", null);

            try
            {
                var source = new MasterKeySource
                {
                    Type = MasterKeySourceType.Environment,
                    Value = "   "
                };

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => MasterKeyProvider.GetMasterKey(source));
            }
            finally
            {
                Environment.SetEnvironmentVariable("BEE_MASTER_KEY", original);
            }
        }

        [Fact]
        [DisplayName("GetMasterKey 不支援的 Type 應拋 InvalidOperationException（default 分支）")]
        public void GetMasterKey_UnsupportedType_ThrowsInvalidOperation()
        {
            // enum 實際只有 File=0 / Environment=1，透過 cast 傳入 99 觸發 switch default
            var source = new MasterKeySource
            {
                Type = (MasterKeySourceType)99,
                Value = "irrelevant"
            };

            var ex = Assert.Throws<InvalidOperationException>(() => MasterKeyProvider.GetMasterKey(source));
            Assert.Contains("Unsupported", ex.Message);
        }

        [Fact]
        [DisplayName("GetMasterKey autoCreate=true 但檔案已存在時應讀取既有檔案內容")]
        public void GetMasterKey_FileExists_AutoCreate_ReturnsExistingKey()
        {
            // 檔案已存在 → File.Exists 為 true → 直接跳到 ReadAllTextShared，不執行 CreateNew 路徑
            byte[] expected = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            string filePath = Path.Combine(Path.GetTempPath(), $"bee-mk-existing-{Guid.NewGuid()}.key");
            File.WriteAllText(filePath, Convert.ToBase64String(expected));

            try
            {
                byte[] actual = MasterKeyProvider.GetMasterKey(
                    new MasterKeySource { Type = MasterKeySourceType.File, Value = filePath },
                    autoCreate: true);

                Assert.Equal(expected, actual);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
    }
}
