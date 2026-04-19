using System.ComponentModel;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// DefinePathInfo 定義檔案路徑組合測試。
    /// 每個測試自行設定並復原 <see cref="BackendInfo.DefinePath"/>，
    /// 使用共享的 Initialize 集合避免與其他使用全域狀態的測試平行執行。
    /// </summary>
    [Collection("Initialize")]
    public class DefinePathInfoTests
    {
        private const string TestRoot = "/tmp/bee-define-tests";

        [Fact]
        [DisplayName("GetSystemSettingsFilePath 應回傳定義根目錄下的 SystemSettings.xml")]
        public void GetSystemSettingsFilePath_ValidDefinePath_ReturnsExpectedPath()
        {
            WithDefinePath(TestRoot, () =>
            {
                var path = DefinePathInfo.GetSystemSettingsFilePath();
                Assert.Equal(Path.Combine(TestRoot, "SystemSettings.xml"), path);
            });
        }

        [Fact]
        [DisplayName("GetDatabaseSettingsFilePath 應回傳定義根目錄下的 DatabaseSettings.xml")]
        public void GetDatabaseSettingsFilePath_ValidDefinePath_ReturnsExpectedPath()
        {
            WithDefinePath(TestRoot, () =>
            {
                var path = DefinePathInfo.GetDatabaseSettingsFilePath();
                Assert.Equal(Path.Combine(TestRoot, "DatabaseSettings.xml"), path);
            });
        }

        [Fact]
        [DisplayName("GetProgramSettingsFilePath 應回傳定義根目錄下的 ProgramSettings.xml")]
        public void GetProgramSettingsFilePath_ValidDefinePath_ReturnsExpectedPath()
        {
            WithDefinePath(TestRoot, () =>
            {
                var path = DefinePathInfo.GetProgramSettingsFilePath();
                Assert.Equal(Path.Combine(TestRoot, "ProgramSettings.xml"), path);
            });
        }

        [Fact]
        [DisplayName("GetDbTableSettingsFilePath 應回傳定義根目錄下的 DbSchemaSettings.xml")]
        public void GetDbTableSettingsFilePath_ValidDefinePath_ReturnsExpectedPath()
        {
            WithDefinePath(TestRoot, () =>
            {
                var path = DefinePathInfo.GetDbTableSettingsFilePath();
                Assert.Equal(Path.Combine(TestRoot, "DbSchemaSettings.xml"), path);
            });
        }

        [Fact]
        [DisplayName("GetTableSchemaFilePath 應組合 TableSchema/<db>/<table>.TableSchema.xml")]
        public void GetTableSchemaFilePath_ValidInput_ReturnsExpectedPath()
        {
            WithDefinePath(TestRoot, () =>
            {
                var path = DefinePathInfo.GetTableSchemaFilePath("common", "Employee");
                var expected = Path.Combine(TestRoot, "TableSchema", "common", "Employee.TableSchema.xml");
                Assert.Equal(expected, path);
            });
        }

        [Fact]
        [DisplayName("GetFormSchemaFilePath 應組合 FormSchema/<progId>.FormSchema.xml")]
        public void GetFormSchemaFilePath_ValidInput_ReturnsExpectedPath()
        {
            WithDefinePath(TestRoot, () =>
            {
                var path = DefinePathInfo.GetFormSchemaFilePath("Employee");
                var expected = Path.Combine(TestRoot, "FormSchema", "Employee.FormSchema.xml");
                Assert.Equal(expected, path);
            });
        }

        [Fact]
        [DisplayName("GetFormLayoutFilePath 應組合 FormLayout/<layoutId>.FormLayout.xml")]
        public void GetFormLayoutFilePath_ValidInput_ReturnsExpectedPath()
        {
            WithDefinePath(TestRoot, () =>
            {
                var path = DefinePathInfo.GetFormLayoutFilePath("EmployeeDefault");
                var expected = Path.Combine(TestRoot, "FormLayout", "EmployeeDefault.FormLayout.xml");
                Assert.Equal(expected, path);
            });
        }

        /// <summary>
        /// 暫時設定 <see cref="BackendInfo.DefinePath"/>，並在 action 執行完畢後復原原值。
        /// </summary>
        private static void WithDefinePath(string definePath, Action action)
        {
            var original = BackendInfo.DefinePath;
            try
            {
                BackendInfo.DefinePath = definePath;
                action();
            }
            finally
            {
                BackendInfo.DefinePath = original;
            }
        }
    }
}
