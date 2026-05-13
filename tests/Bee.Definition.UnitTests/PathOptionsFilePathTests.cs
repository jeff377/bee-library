using System.ComponentModel;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// <see cref="PathOptions"/> 檔案路徑組合測試。直接針對 PathOptions instance 進行，
    /// 不操弄 <see cref="DefinePathInfo"/> 等 process-wide static，可與其他 test class 平行執行。
    /// </summary>
    /// <remarks>
    /// 早期 <c>DefinePathInfoTests</c> 透過 try/finally 切換 <see cref="DefinePathInfo.CurrentOptions"/>
    /// 全域狀態，必須掛 <c>[Collection("Initialize")]</c> 才能避免 race。Phase 5 PR 5.4f 將其改為
    /// 純 PathOptions instance 測試 —— DefinePathInfo 既只是 thin facade（每個 method 都直接 delegate
    /// 給 PathOptions），測 PathOptions 即同等覆蓋。DefinePathInfo facade 本身將於 PR 5.7 刪除。
    /// </remarks>
    public class PathOptionsFilePathTests
    {
        private const string TestRoot = "/tmp/bee-define-tests";

        [Fact]
        [DisplayName("GetSystemSettingsFilePath 應回傳定義根目錄下的 SystemSettings.xml")]
        public void GetSystemSettingsFilePath_ValidDefinePath_ReturnsExpectedPath()
        {
            var paths = new PathOptions { DefinePath = TestRoot };
            Assert.Equal(Path.Combine(TestRoot, "SystemSettings.xml"), paths.GetSystemSettingsFilePath());
        }

        [Fact]
        [DisplayName("GetDatabaseSettingsFilePath 應回傳定義根目錄下的 DatabaseSettings.xml")]
        public void GetDatabaseSettingsFilePath_ValidDefinePath_ReturnsExpectedPath()
        {
            var paths = new PathOptions { DefinePath = TestRoot };
            Assert.Equal(Path.Combine(TestRoot, "DatabaseSettings.xml"), paths.GetDatabaseSettingsFilePath());
        }

        [Fact]
        [DisplayName("GetProgramSettingsFilePath 應回傳定義根目錄下的 ProgramSettings.xml")]
        public void GetProgramSettingsFilePath_ValidDefinePath_ReturnsExpectedPath()
        {
            var paths = new PathOptions { DefinePath = TestRoot };
            Assert.Equal(Path.Combine(TestRoot, "ProgramSettings.xml"), paths.GetProgramSettingsFilePath());
        }

        [Fact]
        [DisplayName("GetDbCategorySettingsFilePath 應回傳定義根目錄下的 DbCategorySettings.xml")]
        public void GetDbCategorySettingsFilePath_ValidDefinePath_ReturnsExpectedPath()
        {
            var paths = new PathOptions { DefinePath = TestRoot };
            Assert.Equal(Path.Combine(TestRoot, "DbCategorySettings.xml"), paths.GetDbCategorySettingsFilePath());
        }

        [Fact]
        [DisplayName("GetTableSchemaFilePath 應組合 TableSchema/<categoryId>/<table>.TableSchema.xml")]
        public void GetTableSchemaFilePath_ValidInput_ReturnsExpectedPath()
        {
            var paths = new PathOptions { DefinePath = TestRoot };
            var expected = Path.Combine(TestRoot, "TableSchema", "common", "Employee.TableSchema.xml");
            Assert.Equal(expected, paths.GetTableSchemaFilePath("common", "Employee"));
        }

        [Fact]
        [DisplayName("GetFormSchemaFilePath 應組合 FormSchema/<progId>.FormSchema.xml")]
        public void GetFormSchemaFilePath_ValidInput_ReturnsExpectedPath()
        {
            var paths = new PathOptions { DefinePath = TestRoot };
            var expected = Path.Combine(TestRoot, "FormSchema", "Employee.FormSchema.xml");
            Assert.Equal(expected, paths.GetFormSchemaFilePath("Employee"));
        }

        [Fact]
        [DisplayName("GetFormLayoutFilePath 應組合 FormLayout/<layoutId>.FormLayout.xml")]
        public void GetFormLayoutFilePath_ValidInput_ReturnsExpectedPath()
        {
            var paths = new PathOptions { DefinePath = TestRoot };
            var expected = Path.Combine(TestRoot, "FormLayout", "EmployeeDefault.FormLayout.xml");
            Assert.Equal(expected, paths.GetFormLayoutFilePath("EmployeeDefault"));
        }
    }
}
