using System.ComponentModel;
using System.IO;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests.Storage
{
    /// <summary>
    /// FileDefineStorage 讀寫 XML 檔案的行為測試。
    /// 各測試使用隔離的臨時目錄做為 BackendInfo.DefinePath，避免互相汙染。
    /// </summary>
    [Collection("Initialize")]
    public class FileDefineStorageTests
    {
        [Fact]
        [DisplayName("SaveFormSchema / GetFormSchema 應可寫入後讀回相同結構")]
        public void SaveAndGetFormSchema_RoundTrips()
        {
            WithTempDefinePath(() =>
            {
                // Arrange
                var storage = new FileDefineStorage();
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
            WithTempDefinePath(() =>
            {
                // Arrange
                var storage = new FileDefineStorage();

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetFormSchema("nonexistent"));
            });
        }

        [Fact]
        [DisplayName("SaveTableSchema / GetTableSchema 應可寫入後讀回")]
        public void SaveAndGetTableSchema_RoundTrips()
        {
            WithTempDefinePath(() =>
            {
                // Arrange
                var storage = new FileDefineStorage();
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
            WithTempDefinePath(() =>
            {
                // Arrange
                var storage = new FileDefineStorage();

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetTableSchema("common", "missing"));
            });
        }

        [Fact]
        [DisplayName("SaveFormLayout / GetFormLayout 應可寫入後讀回")]
        public void SaveAndGetFormLayout_RoundTrips()
        {
            WithTempDefinePath(() =>
            {
                // Arrange
                var storage = new FileDefineStorage();
                var layout = new FormLayout { LayoutId = "DemoLayout", DisplayName = "示範" };

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
            WithTempDefinePath(() =>
            {
                // Arrange
                var storage = new FileDefineStorage();

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetFormLayout("missing"));
            });
        }

        [Fact]
        [DisplayName("SaveDbSchemaSettings / GetDbSchemaSettings 應可寫入後讀回")]
        public void SaveAndGetDbSchemaSettings_RoundTrips()
        {
            WithTempDefinePath(() =>
            {
                // Arrange
                var storage = new FileDefineStorage();
                var settings = new DbSchemaSettings();

                // Act
                storage.SaveDbSchemaSettings(settings);
                var restored = storage.GetDbSchemaSettings();

                // Assert
                Assert.NotNull(restored);
            });
        }

        [Fact]
        [DisplayName("GetDbSchemaSettings 檔案不存在應拋出 FileNotFoundException")]
        public void GetDbSchemaSettings_FileNotFound_Throws()
        {
            WithTempDefinePath(() =>
            {
                // Arrange
                var storage = new FileDefineStorage();

                // Act & Assert
                Assert.Throws<FileNotFoundException>(() => storage.GetDbSchemaSettings());
            });
        }

        /// <summary>
        /// 暫時將 <see cref="BackendInfo.DefinePath"/> 指向新建立的臨時目錄，
        /// 測試結束後還原原值並刪除目錄。
        /// </summary>
        private static void WithTempDefinePath(Action action)
        {
            var original = BackendInfo.DefinePath;
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-define-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                BackendInfo.DefinePath = tempDir;
                action();
            }
            finally
            {
                BackendInfo.DefinePath = original;
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
