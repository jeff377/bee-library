using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// Tests for <see cref="Defaults"/> — framework default define files embedded
    /// as manifest resources in Bee.Definition.dll.
    /// </summary>
    public class DefaultsTests
    {
        // Expected manifest contents after Phase 1.1 migration:
        // - 11 TableSchemas (6 common + 5 company)
        // - 2 FormSchemas (Department, Employee)
        // - 2 FormLayouts (Department, Employee)
        // - 4 Language resources (Department/Employee × en-US/zh-TW)
        // - 1 DbCategorySettings.xml (minimal — st_* only, no ft_project)
        // - 1 CurrencySettings.xml (curated system currency master)
        // - 1 SystemSettings.xml (template with sensible defaults)
        // - 1 DatabaseSettings.xml (empty stub — connection strings are deployment-specific)
        // Total: 23
        private const int ExpectedEmbeddedCount = 23;

        [Fact]
        [DisplayName("ListEmbedded 應回傳 23 個框架預設檔（11 st_* + 2 FormSchema + 2 FormLayout + 4 Language + 1 DbCategorySettings + 1 CurrencySettings + 1 SystemSettings + 1 DatabaseSettings）")]
        public void ListEmbedded_ReturnsExpectedCount()
        {
            var files = Defaults.ListEmbedded();

            Assert.Equal(ExpectedEmbeddedCount, files.Count);
        }

        [Fact]
        [DisplayName("ListEmbedded 使用 forward-slash 分隔且回傳結果為排序後")]
        public void ListEmbedded_UsesForwardSlashAndSorted()
        {
            var files = Defaults.ListEmbedded();

            Assert.All(files, p => Assert.DoesNotContain('\\', p));
            Assert.Equal(files.OrderBy(x => x, StringComparer.Ordinal), files);
        }

        [Theory]
        [InlineData("DbCategorySettings.xml")]
        [InlineData("CurrencySettings.xml")]
        [InlineData("SystemSettings.xml")]
        [InlineData("DatabaseSettings.xml")]
        [InlineData("TableSchema/common/st_user.TableSchema.xml")]
        [InlineData("TableSchema/company/st_employee.TableSchema.xml")]
        [InlineData("FormSchema/Department.FormSchema.xml")]
        [InlineData("FormLayout/Employee.FormLayout.xml")]
        [InlineData("Language/zh-TW/Department.Language.xml")]
        [DisplayName("ListEmbedded 應包含關鍵框架預設檔")]
        public void ListEmbedded_ContainsKeyFiles(string expected)
        {
            var files = Defaults.ListEmbedded();

            Assert.Contains(expected, files);
        }

        [Fact]
        [DisplayName("OpenEmbedded 對 st_user.TableSchema.xml 可成功 deserialize 為 TableSchema")]
        public void OpenEmbedded_StUserTableSchema_DeserializesSuccessfully()
        {
            var schema = XmlCodec.Deserialize<TableSchema>(ReadEmbedded("TableSchema/common/st_user.TableSchema.xml"));

            Assert.NotNull(schema);
            Assert.Equal("st_user", schema!.TableName);
            Assert.NotEmpty(schema.Fields!);
        }

        [Fact]
        [DisplayName("OpenEmbedded 對 Department.FormSchema.xml 可成功 deserialize")]
        public void OpenEmbedded_DepartmentFormSchema_DeserializesSuccessfully()
        {
            var schema = XmlCodec.Deserialize<FormSchema>(ReadEmbedded("FormSchema/Department.FormSchema.xml"));

            Assert.NotNull(schema);
            Assert.Equal("Department", schema!.ProgId);
        }

        [Fact]
        [DisplayName("OpenEmbedded 對 CurrencySettings.xml 可 deserialize 且位數依幣別（JPY=0、USD=2、BHD=3）")]
        public void OpenEmbedded_CurrencySettings_DeserializesWithCurrencyDecimals()
        {
            var settings = XmlCodec.Deserialize<CurrencySettings>(ReadEmbedded("CurrencySettings.xml"));

            Assert.NotNull(settings);
            Assert.NotEmpty(settings!);
            Assert.Equal(2, settings.GetDecimals("USD"));
            Assert.Equal(0, settings.GetDecimals("JPY"));
            Assert.Equal(3, settings.GetDecimals("BHD"));
        }

        [Fact]
        [DisplayName("OpenEmbedded 對精簡版 DbCategorySettings.xml 只列 st_* 五張 company 表（無 ft_project）")]
        public void OpenEmbedded_DbCategorySettings_HasOnlyStTables()
        {
            var settings = XmlCodec.Deserialize<DbCategorySettings>(ReadEmbedded("DbCategorySettings.xml"));

            Assert.NotNull(settings);
            var company = settings!.Categories!.First(c => c.Id == "company");
            Assert.All(company.Tables!, t => Assert.StartsWith("st_", t.TableName));
            Assert.DoesNotContain(company.Tables!, t => t.TableName == "ft_project");
        }

        [Fact]
        [DisplayName("OpenEmbedded 對 SystemSettings.xml 可成功 deserialize 並具備合理 production 預設（IsDebugMode=false、MasterKeySource=Environment）")]
        public void OpenEmbedded_SystemSettings_HasConservativeDefaults()
        {
            var settings = XmlCodec.Deserialize<SystemSettings>(ReadEmbedded("SystemSettings.xml"));

            Assert.NotNull(settings);
            // 保守預設：debug off、MasterKey 指向 env var（消費者部署時再決定具體值或改 source）
            Assert.False(settings!.CommonConfiguration.IsDebugMode);
            var masterKey = settings.BackendConfiguration.SecurityKeySettings.MasterKeySource;
            Assert.Equal(Bee.Definition.Security.MasterKeySourceType.Environment, masterKey.Type);
            Assert.Equal("BEE_MASTER_KEY", masterKey.Value);
            // ApiPayloadOptions 預設值
            Assert.Equal("messagepack", settings.CommonConfiguration.ApiPayloadOptions.Serializer);
            Assert.Equal("aes-cbc-hmac", settings.CommonConfiguration.ApiPayloadOptions.Encryptor);
        }

        [Fact]
        [DisplayName("OpenEmbedded 對 DatabaseSettings.xml 為空殼（Items 為 null 或空集合）— 連線字串是部署選擇")]
        public void OpenEmbedded_DatabaseSettings_IsEmptyStub()
        {
            var settings = XmlCodec.Deserialize<DatabaseSettings>(ReadEmbedded("DatabaseSettings.xml"));

            Assert.NotNull(settings);
            // Items 與 Servers 在序列化為空時會被 IsSerializeEmpty 短路成 null，
            // deserialize 回來可能是 null 或空集合——兩者都代表「沒有任何 DatabaseItem 預設」。
            Assert.True(settings!.Items == null || settings.Items.Count == 0);
            Assert.True(settings.Servers == null || settings.Servers.Count == 0);
        }

        [Fact]
        [DisplayName("OpenEmbedded 支援 Windows-style backslash 路徑（自動正規化）")]
        public void OpenEmbedded_AcceptsBackslashPath()
        {
            using var stream = Defaults.OpenEmbedded("TableSchema\\common\\st_user.TableSchema.xml");

            Assert.NotNull(stream);
        }

        [Fact]
        [DisplayName("OpenEmbedded 對不存在的 relativePath 應拋 FileNotFoundException")]
        public void OpenEmbedded_UnknownPath_ThrowsFileNotFound()
        {
            Assert.Throws<FileNotFoundException>(
                () => Defaults.OpenEmbedded("TableSchema/common/does_not_exist.TableSchema.xml"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("OpenEmbedded 對 null / 空字串應拋 ArgumentException（含 ArgumentNullException）")]
        public void OpenEmbedded_InvalidArg_ThrowsArgumentException(string? path)
        {
            // ArgumentException.ThrowIfNullOrWhiteSpace 對 null 拋 ArgumentNullException、
            // 對空字串拋 ArgumentException——兩者皆繼承自 ArgumentException，用
            // ThrowsAny 涵蓋。
            Assert.ThrowsAny<ArgumentException>(() => Defaults.OpenEmbedded(path!));
        }

        [Fact]
        [DisplayName("MaterializeTo 對空目錄寫出全部框架預設檔（含子目錄結構）")]
        public void MaterializeTo_EmptyDirectory_WritesAllFiles()
        {
            var tempDir = CreateTempDir();
            try
            {
                var result = Defaults.MaterializeTo(tempDir);

                Assert.Equal(ExpectedEmbeddedCount, result.WrittenCount);
                Assert.Equal(0, result.SkippedCount);

                // 抽樣驗證實際檔案存在
                Assert.True(File.Exists(Path.Combine(tempDir, "DbCategorySettings.xml")));
                Assert.True(File.Exists(Path.Combine(tempDir, "TableSchema", "common", "st_user.TableSchema.xml")));
                Assert.True(File.Exists(Path.Combine(tempDir, "Language", "zh-TW", "Department.Language.xml")));
            }
            finally
            {
                Cleanup(tempDir);
            }
        }

        [Fact]
        [DisplayName("MaterializeTo 預設 Overwrite=false：第二次跑全部 skip")]
        public void MaterializeTo_DefaultOverwriteFalse_SkipsExistingOnSecondRun()
        {
            var tempDir = CreateTempDir();
            try
            {
                Defaults.MaterializeTo(tempDir);
                var secondRun = Defaults.MaterializeTo(tempDir);

                Assert.Equal(0, secondRun.WrittenCount);
                Assert.Equal(ExpectedEmbeddedCount, secondRun.SkippedCount);
            }
            finally
            {
                Cleanup(tempDir);
            }
        }

        [Fact]
        [DisplayName("MaterializeTo Overwrite=true：第二次跑全部 overwrite")]
        public void MaterializeTo_OverwriteTrue_RewritesExisting()
        {
            var tempDir = CreateTempDir();
            try
            {
                Defaults.MaterializeTo(tempDir);

                // 故意把第一個檔覆寫為空字串，模擬使用者改動
                var sentinelFile = Path.Combine(tempDir, "DbCategorySettings.xml");
                File.WriteAllText(sentinelFile, string.Empty);

                var secondRun = Defaults.MaterializeTo(tempDir, new MaterializeOptions { Overwrite = true });

                Assert.Equal(ExpectedEmbeddedCount, secondRun.WrittenCount);
                Assert.Equal(0, secondRun.SkippedCount);
                Assert.True(new FileInfo(sentinelFile).Length > 0);
            }
            finally
            {
                Cleanup(tempDir);
            }
        }

        [Fact]
        [DisplayName("MaterializeTo 寫出後 SystemSettings.xml / DatabaseSettings.xml 也應存在")]
        public void MaterializeTo_WritesSystemAndDatabaseSettings()
        {
            var tempDir = CreateTempDir();
            try
            {
                Defaults.MaterializeTo(tempDir);

                Assert.True(File.Exists(Path.Combine(tempDir, "SystemSettings.xml")));
                Assert.True(File.Exists(Path.Combine(tempDir, "DatabaseSettings.xml")));
            }
            finally
            {
                Cleanup(tempDir);
            }
        }

        [Fact]
        [DisplayName("MaterializeTo Filter 限縮為 TableSchema 子集應只寫 11 檔")]
        public void MaterializeTo_FilterTableSchemaOnly_WritesEleven()
        {
            var tempDir = CreateTempDir();
            try
            {
                var options = new MaterializeOptions
                {
                    Filter = p => p.StartsWith("TableSchema/", StringComparison.Ordinal),
                };

                var result = Defaults.MaterializeTo(tempDir, options);

                Assert.Equal(11, result.WrittenCount);
                Assert.All(result.WrittenRelativePaths, p => Assert.StartsWith("TableSchema/", p));
            }
            finally
            {
                Cleanup(tempDir);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("MaterializeTo 對 null / 空字串路徑應拋 ArgumentException（含 ArgumentNullException）")]
        public void MaterializeTo_InvalidPath_ThrowsArgumentException(string? path)
        {
            Assert.ThrowsAny<ArgumentException>(() => Defaults.MaterializeTo(path!));
        }

        [Fact]
        [DisplayName("MaterializeTo 對不存在的目錄會自動建立")]
        public void MaterializeTo_NonexistentDirectory_CreatesIt()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-defaults-{Guid.NewGuid():N}");
            // 故意不 Directory.CreateDirectory
            try
            {
                var result = Defaults.MaterializeTo(tempDir);

                Assert.True(Directory.Exists(tempDir));
                Assert.Equal(ExpectedEmbeddedCount, result.WrittenCount);
            }
            finally
            {
                Cleanup(tempDir);
            }
        }

        [Fact]
        [DisplayName("MaterializeResult.WrittenRelativePaths 與 SkippedRelativePaths 加總應等於 ExpectedEmbeddedCount（minus Filter 排除）")]
        public void MaterializeResult_WrittenPlusSkipped_EqualsTotal()
        {
            var tempDir = CreateTempDir();
            try
            {
                var first = Defaults.MaterializeTo(tempDir);
                Assert.Equal(ExpectedEmbeddedCount, first.WrittenRelativePaths.Count + first.SkippedRelativePaths.Count);

                var second = Defaults.MaterializeTo(tempDir);
                Assert.Equal(ExpectedEmbeddedCount, second.WrittenRelativePaths.Count + second.SkippedRelativePaths.Count);
            }
            finally
            {
                Cleanup(tempDir);
            }
        }

        private static string ReadEmbedded(string relativePath)
        {
            using var stream = Defaults.OpenEmbedded(relativePath);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static string CreateTempDir()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-defaults-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        private static void Cleanup(string tempDir)
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (IOException)
            {
                // best effort
            }
        }
    }
}
