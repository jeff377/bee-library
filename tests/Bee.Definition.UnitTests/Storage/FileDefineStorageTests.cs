using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests.Storage
{
    /// <summary>
    /// FileDefineStorage 讀寫 XML 檔案的行為測試。
    /// 各測試使用隔離的臨時目錄做為 DefinePath（透過 <c>WithTempDefinePath</c>），
    /// 不操弄 <see cref="DefinePathInfo"/> 等 process-wide static，可與其他 test class 平行執行。
    /// </summary>
    public class FileDefineStorageTests
    {
        [Fact]
        [DisplayName("SaveFormSchema / GetFormSchema 應可寫入後讀回相同結構")]
        public void SaveAndGetFormSchema_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);
                var schema = new FormSchema("Demo", "示範");
                var table = schema.Tables!.Add("Demo", "示範");
                table.Fields!.Add("sys_id", "編號", FieldDbType.String);

                // Act
                storage.SaveFormSchema(schema);
                var restored = storage.GetFormSchema("Demo");

                // Assert
                Assert.NotNull(restored);
                Assert.Equal("Demo", restored!.ProgId);
                Assert.Equal("示範", restored.DisplayName);
            });
        }

        [Fact]
        [DisplayName("GetFormSchema 檔案不存在應拋出 FileNotFoundException")]
        public void GetFormSchema_FileNotFound_Throws()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetFormSchema("nonexistent"));
            });
        }

        [Fact]
        [DisplayName("SaveTableSchema / GetTableSchema 應可寫入後讀回")]
        public void SaveAndGetTableSchema_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);
                var schema = new TableSchema { TableName = "ft_demo", DisplayName = "Demo" };
                schema.Fields!.Add("sys_no", "流水號", FieldDbType.AutoIncrement);

                // Act
                storage.SaveTableSchema("common", schema);
                var restored = storage.GetTableSchema("common", "ft_demo");

                // Assert
                Assert.NotNull(restored);
                Assert.Equal("ft_demo", restored!.TableName);
                Assert.Single(restored.Fields!);
            });
        }

        [Fact]
        [DisplayName("GetTableSchema 檔案不存在應拋出 FileNotFoundException")]
        public void GetTableSchema_FileNotFound_Throws()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetTableSchema("common", "missing"));
            });
        }

        [Fact]
        [DisplayName("SaveFormLayout / GetFormLayout 應可寫入後讀回")]
        public void SaveAndGetFormLayout_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);
                var layout = new FormLayout { LayoutId = "DemoLayout", Caption = "示範" };

                // Act
                storage.SaveFormLayout(layout);
                var restored = storage.GetFormLayout("DemoLayout");

                // Assert
                Assert.NotNull(restored);
                Assert.Equal("DemoLayout", restored!.LayoutId);
            });
        }

        [Fact]
        [DisplayName("GetFormLayout 檔案不存在應拋出 FileNotFoundException")]
        public void GetFormLayout_FileNotFound_Throws()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetFormLayout("missing"));
            });
        }

        [Fact]
        [DisplayName("SaveDbCategorySettings / GetDbCategorySettings 應可寫入後讀回")]
        public void SaveAndGetDbCategorySettings_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);
                var settings = new DbCategorySettings();

                // Act
                storage.SaveDbCategorySettings(settings);
                var restored = storage.GetDbCategorySettings();

                // Assert
                Assert.NotNull(restored);
            });
        }

        [Fact]
        [DisplayName("GetDbCategorySettings 檔案不存在應拋出 FileNotFoundException")]
        public void GetDbCategorySettings_FileNotFound_Throws()
        {
            WithTempDefinePath(paths =>
            {
                // Arrange
                var storage = new FileDefineStorage(paths);

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetDbCategorySettings());
            });
        }

        [Fact]
        [DisplayName("SaveProgramSettings / GetProgramSettings 應可寫入後讀回")]
        public void SaveAndGetProgramSettings_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                var storage = new FileDefineStorage(paths);
                var settings = new ProgramSettings();

                storage.SaveProgramSettings(settings);
                var restored = storage.GetProgramSettings();

                Assert.NotNull(restored);
            });
        }

        [Fact]
        [DisplayName("GetProgramSettings 檔案不存在應拋出 FileNotFoundException")]
        public void GetProgramSettings_FileNotFound_Throws()
        {
            WithTempDefinePath(paths =>
            {
                var storage = new FileDefineStorage(paths);
                Assert.Throws<FileNotFoundException>(() => storage.GetProgramSettings());
            });
        }

        [Fact]
        [DisplayName("SaveLanguage / GetLanguage 應可寫入後讀回相同語言資源")]
        public void SaveAndGetLanguage_RoundTrips()
        {
            WithTempDefinePath(paths =>
            {
                var storage = new FileDefineStorage(paths);
                var resource = new LanguageResource { Lang = "en", Namespace = "Core" };

                storage.SaveLanguage(resource);
                var restored = storage.GetLanguage("en", "Core");

                Assert.NotNull(restored);
                Assert.Equal("en", restored!.Lang);
                Assert.Equal("Core", restored.Namespace);
            });
        }

        [Fact]
        [DisplayName("GetLanguage 檔案不存在應回傳 null（非拋例外）")]
        public void GetLanguage_FileNotFound_ReturnsNull()
        {
            WithTempDefinePath(paths =>
            {
                var storage = new FileDefineStorage(paths);
                var result = storage.GetLanguage("zh-TW", "Missing");
                Assert.Null(result);
            });
        }

        /// <summary>
        /// 建立新的臨時目錄並把對應的 <see cref="PathOptions"/> 傳給 <paramref name="action"/>，
        /// 測試結束後刪除目錄。Tests inject the supplied <see cref="PathOptions"/> directly into
        /// <see cref="FileDefineStorage"/> rather than relying on the shared
        /// <see cref="DefinePathInfo"/> static facade.
        /// </summary>
        private static void WithTempDefinePath(Action<PathOptions> action)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-define-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                action(new PathOptions { DefinePath = tempDir });
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
