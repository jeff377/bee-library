using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="LocalDefineAccess"/> 所有 Save 方法的覆蓋測試。
    /// 以暫存 DefinePath 隔離測試檔案，避免污染共用的 tests/Define 目錄。
    /// 各測試內以 <see cref="TempDefinePath.Options"/> 構造 <see cref="LocalDefineAccess"/>，
    /// 寫入目標即為當次測試的隔離暫存區。
    /// </summary>
    [Collection("Initialize")]
    public class LocalDefineAccessSaveTests
    {
        private static readonly string[] DbViaDefineKeys = { "db_via_define" };

        private static LocalDefineAccess CreateAccess(PathOptions paths)
            => new LocalDefineAccess(new FileDefineStorage(paths), paths);

        [Fact]
        [DisplayName("SaveSystemSettings 應寫入 SystemSettings.xml 並可再讀回")]
        public void SaveSystemSettings_WritesFile()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            var settings = new SystemSettings();
            settings.BackendConfiguration.ApiKey = "saved_id";

            access.SaveSystemSettings(settings);

            var filePath = temp.Options.GetSystemSettingsFilePath();
            Assert.True(File.Exists(filePath));
            Assert.Contains("saved_id", File.ReadAllText(filePath));
        }

        [Fact]
        [DisplayName("SaveDatabaseSettings 應寫入 DatabaseSettings.xml")]
        public void SaveDatabaseSettings_WritesFile()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            var settings = new DatabaseSettings();

            access.SaveDatabaseSettings(settings);

            Assert.True(File.Exists(temp.Options.GetDatabaseSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveProgramSettings 應寫入 ProgramSettings.xml")]
        public void SaveProgramSettings_WritesFile()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            var settings = new ProgramSettings();

            access.SaveProgramSettings(settings);

            Assert.True(File.Exists(temp.Options.GetProgramSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDbCategorySettings 應透過 DefineStorage 寫入 DbCategorySettings.xml")]
        public void SaveDbCategorySettings_WritesFile()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            var settings = new DbCategorySettings();

            access.SaveDbCategorySettings(settings);

            Assert.True(File.Exists(temp.Options.GetDbCategorySettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveTableSchema 應寫入對應資料庫資料夾下的 TableSchema xml")]
        public void SaveTableSchema_WritesFile()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            var schema = new TableSchema { TableName = "t_sample" };

            access.SaveTableSchema("dbX", schema);

            Assert.True(File.Exists(temp.Options.GetTableSchemaFilePath("dbX", "t_sample")));
        }

        [Fact]
        [DisplayName("SaveFormSchema 應寫入以 ProgId 命名的 FormSchema xml")]
        public void SaveFormSchema_WritesFile()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            var schema = new FormSchema { ProgId = "P_Test", CategoryId = "common" };

            access.SaveFormSchema(schema);

            Assert.True(File.Exists(temp.Options.GetFormSchemaFilePath("P_Test")));
        }

        [Fact]
        [DisplayName("SaveFormSchema 缺少 CategoryId 時應丟 InvalidOperationException")]
        public void SaveFormSchema_ThrowsWhenCategoryIdEmpty()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            var schema = new FormSchema { ProgId = "P_NoCategory" };

            var ex = Assert.Throws<InvalidOperationException>(() => access.SaveFormSchema(schema));
            Assert.Contains("P_NoCategory", ex.Message);
            Assert.Contains("CategoryId", ex.Message);
        }

        [Fact]
        [DisplayName("SaveFormLayout 應寫入以 LayoutId 命名的 FormLayout xml")]
        public void SaveFormLayout_WritesFile()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            var layout = new FormLayout { LayoutId = "L_Test" };

            access.SaveFormLayout(layout);

            Assert.True(File.Exists(temp.Options.GetFormLayoutFilePath("L_Test")));
        }

        // NOTE: cache-roundtrip 整合測試（Save → cache miss → reload via cache layer）刪除於
        // PR 5.2。原因：cache 層的 FileDefineStorage 由 CacheContainer.Initialize 構造時鎖定
        // 在 GlobalFixture 的 PathOptions，與 LocalDefineAccess(temp.Options) 的儲存路徑不一致。
        // PR 5.3 將 CacheContainer 改為 DI singleton 並讓 cache classes 接 PathOptions 後，重新引入。

        [Fact]
        [DisplayName("SaveDefine(SystemSettings) 應委派至 SaveSystemSettings")]
        public void SaveDefine_SystemSettings_DelegatesToSaveSystemSettings()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            access.SaveDefine(DefineType.SystemSettings, new SystemSettings());
            Assert.True(File.Exists(temp.Options.GetSystemSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(DatabaseSettings) 應委派至 SaveDatabaseSettings")]
        public void SaveDefine_DatabaseSettings_DelegatesToSaveDatabaseSettings()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            access.SaveDefine(DefineType.DatabaseSettings, new DatabaseSettings());
            Assert.True(File.Exists(temp.Options.GetDatabaseSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(ProgramSettings) 應委派至 SaveProgramSettings")]
        public void SaveDefine_ProgramSettings_DelegatesToSaveProgramSettings()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            access.SaveDefine(DefineType.ProgramSettings, new ProgramSettings());
            Assert.True(File.Exists(temp.Options.GetProgramSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(DbCategorySettings) 應委派至 SaveDbCategorySettings")]
        public void SaveDefine_DbCategorySettings_DelegatesToSaveDbCategorySettings()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            access.SaveDefine(DefineType.DbCategorySettings, new DbCategorySettings());
            Assert.True(File.Exists(temp.Options.GetDbCategorySettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(TableSchema) 帶單一 key 應委派至 SaveTableSchema")]
        public void SaveDefine_TableSchema_WithKey_DelegatesToSaveTableSchema()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            var schema = new TableSchema { TableName = "t_via_define" };
            access.SaveDefine(DefineType.TableSchema, schema, DbViaDefineKeys);
            Assert.True(File.Exists(temp.Options.GetTableSchemaFilePath("db_via_define", "t_via_define")));
        }

        [Fact]
        [DisplayName("SaveDefine(FormLayout) 應委派至 SaveFormLayout")]
        public void SaveDefine_FormLayout_DelegatesToSaveFormLayout()
        {
            using var temp = new TempDefinePath();
            var access = CreateAccess(temp.Options);
            var layout = new FormLayout { LayoutId = "L_via_define" };
            access.SaveDefine(DefineType.FormLayout, layout);
            Assert.True(File.Exists(temp.Options.GetFormLayoutFilePath("L_via_define")));
        }
    }
}
