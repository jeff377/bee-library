using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="LocalDefineAccess"/> 所有 Save 方法的覆蓋測試。
    /// 以暫存 DefinePath 隔離測試檔案，避免污染共用的 tests/Define 目錄。
    /// </summary>
    [Collection("Initialize")]
    public class LocalDefineAccessSaveTests
    {
        private static readonly string[] DbViaDefineKeys = { "db_via_define" };

        private readonly LocalDefineAccess _access = new LocalDefineAccess();

        /// <summary>
        /// 建立暫存 DefinePath 並將 <see cref="BackendInfo.DefinePath"/> 切過去；
        /// Dispose 時還原並清除暫存目錄。
        /// </summary>
        private sealed class TempDefinePath : IDisposable
        {
            private readonly string _original;
            public string Path { get; }

            public TempDefinePath()
            {
                _original = BackendInfo.DefinePath;
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bee-lda-{Guid.NewGuid():N}");
                Directory.CreateDirectory(Path);
                BackendInfo.DefinePath = Path;
            }

            public void Dispose()
            {
                BackendInfo.DefinePath = _original;
                try
                {
                    if (Directory.Exists(Path))
                        Directory.Delete(Path, recursive: true);
                }
                catch (IOException) { /* 測試完整性優先於暫存清理 */ }
            }
        }

        [Fact]
        [DisplayName("SaveSystemSettings 應寫入 SystemSettings.xml 並可再讀回")]
        public void SaveSystemSettings_WritesFile()
        {
            using var temp = new TempDefinePath();
            var settings = new SystemSettings();
            settings.BackendConfiguration.DatabaseId = "saved_id";

            _access.SaveSystemSettings(settings);

            var filePath = DefinePathInfo.GetSystemSettingsFilePath();
            Assert.True(File.Exists(filePath));
            Assert.Contains("saved_id", File.ReadAllText(filePath));
        }

        [Fact]
        [DisplayName("SaveDatabaseSettings 應寫入 DatabaseSettings.xml")]
        public void SaveDatabaseSettings_WritesFile()
        {
            using var temp = new TempDefinePath();
            var settings = new DatabaseSettings();

            _access.SaveDatabaseSettings(settings);

            Assert.True(File.Exists(DefinePathInfo.GetDatabaseSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveProgramSettings 應寫入 ProgramSettings.xml")]
        public void SaveProgramSettings_WritesFile()
        {
            using var temp = new TempDefinePath();
            var settings = new ProgramSettings();

            _access.SaveProgramSettings(settings);

            Assert.True(File.Exists(DefinePathInfo.GetProgramSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDbSchemaSettings 應透過 DefineStorage 寫入 DbSchemaSettings.xml")]
        public void SaveDbSchemaSettings_WritesFile()
        {
            using var temp = new TempDefinePath();
            var settings = new DbSchemaSettings();

            _access.SaveDbSchemaSettings(settings);

            Assert.True(File.Exists(DefinePathInfo.GetDbTableSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveTableSchema 應寫入對應資料庫資料夾下的 TableSchema xml")]
        public void SaveTableSchema_WritesFile()
        {
            using var temp = new TempDefinePath();
            var schema = new TableSchema { TableName = "t_sample" };

            _access.SaveTableSchema("dbX", schema);

            Assert.True(File.Exists(DefinePathInfo.GetTableSchemaFilePath("dbX", "t_sample")));
        }

        [Fact]
        [DisplayName("SaveFormSchema 應寫入以 ProgId 命名的 FormSchema xml")]
        public void SaveFormSchema_WritesFile()
        {
            using var temp = new TempDefinePath();
            var schema = new FormSchema { ProgId = "P_Test" };

            _access.SaveFormSchema(schema);

            Assert.True(File.Exists(DefinePathInfo.GetFormSchemaFilePath("P_Test")));
        }

        [Fact]
        [DisplayName("SaveFormLayout 應寫入以 LayoutId 命名的 FormLayout xml")]
        public void SaveFormLayout_WritesFile()
        {
            using var temp = new TempDefinePath();
            var layout = new FormLayout { LayoutId = "L_Test" };

            _access.SaveFormLayout(layout);

            Assert.True(File.Exists(DefinePathInfo.GetFormLayoutFilePath("L_Test")));
        }

        [Fact]
        [DisplayName("Save 後 GetProgramSettings 可經由 ProgramSettingsCache 讀回")]
        public void GetProgramSettings_RoundTrip_ThroughCache()
        {
            using var temp = new TempDefinePath();
            _access.SaveProgramSettings(new ProgramSettings());

            var loaded = _access.GetProgramSettings();
            Assert.NotNull(loaded);
        }

        [Fact]
        [DisplayName("Save 後 GetFormLayout 可經由 FormLayoutCache 讀回")]
        public void GetFormLayout_RoundTrip_ThroughCache()
        {
            using var temp = new TempDefinePath();
            var layout = new FormLayout { LayoutId = "L_Get" };
            _access.SaveFormLayout(layout);

            var loaded = _access.GetFormLayout("L_Get");
            Assert.NotNull(loaded);
            Assert.Equal("L_Get", loaded.LayoutId);
        }

        [Fact]
        [DisplayName("SaveDefine(SystemSettings) 應委派至 SaveSystemSettings")]
        public void SaveDefine_SystemSettings_DelegatesToSaveSystemSettings()
        {
            using var temp = new TempDefinePath();
            _access.SaveDefine(DefineType.SystemSettings, new SystemSettings());
            Assert.True(File.Exists(DefinePathInfo.GetSystemSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(DatabaseSettings) 應委派至 SaveDatabaseSettings")]
        public void SaveDefine_DatabaseSettings_DelegatesToSaveDatabaseSettings()
        {
            using var temp = new TempDefinePath();
            _access.SaveDefine(DefineType.DatabaseSettings, new DatabaseSettings());
            Assert.True(File.Exists(DefinePathInfo.GetDatabaseSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(ProgramSettings) 應委派至 SaveProgramSettings")]
        public void SaveDefine_ProgramSettings_DelegatesToSaveProgramSettings()
        {
            using var temp = new TempDefinePath();
            _access.SaveDefine(DefineType.ProgramSettings, new ProgramSettings());
            Assert.True(File.Exists(DefinePathInfo.GetProgramSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(DbSchemaSettings) 應委派至 SaveDbSchemaSettings")]
        public void SaveDefine_DbSchemaSettings_DelegatesToSaveDbSchemaSettings()
        {
            using var temp = new TempDefinePath();
            _access.SaveDefine(DefineType.DbSchemaSettings, new DbSchemaSettings());
            Assert.True(File.Exists(DefinePathInfo.GetDbTableSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(TableSchema) 帶單一 key 應委派至 SaveTableSchema")]
        public void SaveDefine_TableSchema_WithKey_DelegatesToSaveTableSchema()
        {
            using var temp = new TempDefinePath();
            var schema = new TableSchema { TableName = "t_via_define" };
            _access.SaveDefine(DefineType.TableSchema, schema, DbViaDefineKeys);
            Assert.True(File.Exists(DefinePathInfo.GetTableSchemaFilePath("db_via_define", "t_via_define")));
        }

        [Fact]
        [DisplayName("SaveDefine(FormLayout) 應委派至 SaveFormLayout")]
        public void SaveDefine_FormLayout_DelegatesToSaveFormLayout()
        {
            using var temp = new TempDefinePath();
            var layout = new FormLayout { LayoutId = "L_via_define" };
            _access.SaveDefine(DefineType.FormLayout, layout);
            Assert.True(File.Exists(DefinePathInfo.GetFormLayoutFilePath("L_via_define")));
        }
    }
}
