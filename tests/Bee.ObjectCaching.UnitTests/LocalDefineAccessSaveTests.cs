using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="LocalDefineAccess"/> 所有 Save 方法的覆蓋測試。
    /// 各測試以本地 <see cref="TempDir"/> 隔離 <see cref="PathOptions"/>，直接傳給
    /// <see cref="LocalDefineAccess"/> ctor —— 不操弄 <see cref="DefinePathInfo"/>
    /// process-wide static，可與其他 test class 平行執行。
    /// </summary>
    /// <remarks>
    /// Save 路徑會呼叫 <c>CacheContainer.X.Remove(key)</c> 失效 process-wide 快取，但 keys
    /// 皆為本測試特有（如 <c>dbX/t_sample</c>、<c>P_Test</c>），不會影響其他測試。
    /// </remarks>
    public class LocalDefineAccessSaveTests
    {
        private static readonly string[] DbViaDefineKeys = { "db_via_define" };
        private static readonly string[] s_formLayoutKey = { "L_GetTest" };
        private static readonly string[] s_languageKeys = { "zh-TW", "Common" };

        private static LocalDefineAccess CreateAccess(PathOptions paths)
            => new LocalDefineAccess(new FileDefineStorage(paths), paths);

        [Fact]
        [DisplayName("SaveSystemSettings 應寫入 SystemSettings.xml 並可再讀回")]
        public void SaveSystemSettings_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var settings = new SystemSettings();
            settings.BackendConfiguration.SecurityKeySettings.MasterKeySource.Value = "saved_id";

            access.SaveSystemSettings(settings);

            var filePath = temp.Options.GetSystemSettingsFilePath();
            Assert.True(File.Exists(filePath));
            Assert.Contains("saved_id", File.ReadAllText(filePath));
        }

        [Fact]
        [DisplayName("SaveDatabaseSettings 應寫入 DatabaseSettings.xml")]
        public void SaveDatabaseSettings_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var settings = new DatabaseSettings();

            access.SaveDatabaseSettings(settings);

            Assert.True(File.Exists(temp.Options.GetDatabaseSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveProgramSettings 應寫入 ProgramSettings.xml")]
        public void SaveProgramSettings_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var settings = new ProgramSettings();

            access.SaveProgramSettings(settings);

            Assert.True(File.Exists(temp.Options.GetProgramSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDbCategorySettings 應透過 DefineStorage 寫入 DbCategorySettings.xml")]
        public void SaveDbCategorySettings_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var settings = new DbCategorySettings();

            access.SaveDbCategorySettings(settings);

            Assert.True(File.Exists(temp.Options.GetDbCategorySettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveTableSchema 應寫入對應資料庫資料夾下的 TableSchema xml")]
        public void SaveTableSchema_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var schema = new TableSchema { TableName = "t_sample" };

            access.SaveTableSchema("dbX", schema);

            Assert.True(File.Exists(temp.Options.GetTableSchemaFilePath("dbX", "t_sample")));
        }

        [Fact]
        [DisplayName("SaveFormSchema 應寫入以 ProgId 命名的 FormSchema xml")]
        public void SaveFormSchema_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var schema = new FormSchema { ProgId = "P_Test", CategoryId = "common" };

            access.SaveFormSchema(schema);

            Assert.True(File.Exists(temp.Options.GetFormSchemaFilePath("P_Test")));
        }

        [Fact]
        [DisplayName("SaveFormSchema 缺少 CategoryId 時應丟 InvalidOperationException")]
        public void SaveFormSchema_ThrowsWhenCategoryIdEmpty()
        {
            using var temp = TempDir.Create();
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
            using var temp = TempDir.Create();
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
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            access.SaveDefine(DefineType.SystemSettings, new SystemSettings());
            Assert.True(File.Exists(temp.Options.GetSystemSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(DatabaseSettings) 應委派至 SaveDatabaseSettings")]
        public void SaveDefine_DatabaseSettings_DelegatesToSaveDatabaseSettings()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            access.SaveDefine(DefineType.DatabaseSettings, new DatabaseSettings());
            Assert.True(File.Exists(temp.Options.GetDatabaseSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(ProgramSettings) 應委派至 SaveProgramSettings")]
        public void SaveDefine_ProgramSettings_DelegatesToSaveProgramSettings()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            access.SaveDefine(DefineType.ProgramSettings, new ProgramSettings());
            Assert.True(File.Exists(temp.Options.GetProgramSettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(DbCategorySettings) 應委派至 SaveDbCategorySettings")]
        public void SaveDefine_DbCategorySettings_DelegatesToSaveDbCategorySettings()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            access.SaveDefine(DefineType.DbCategorySettings, new DbCategorySettings());
            Assert.True(File.Exists(temp.Options.GetDbCategorySettingsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(TableSchema) 帶單一 key 應委派至 SaveTableSchema")]
        public void SaveDefine_TableSchema_WithKey_DelegatesToSaveTableSchema()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var schema = new TableSchema { TableName = "t_via_define" };
            access.SaveDefine(DefineType.TableSchema, schema, DbViaDefineKeys);
            Assert.True(File.Exists(temp.Options.GetTableSchemaFilePath("db_via_define", "t_via_define")));
        }

        [Fact]
        [DisplayName("SaveDefine(FormLayout) 應委派至 SaveFormLayout")]
        public void SaveDefine_FormLayout_DelegatesToSaveFormLayout()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var layout = new FormLayout { LayoutId = "L_via_define" };
            access.SaveDefine(DefineType.FormLayout, layout);
            Assert.True(File.Exists(temp.Options.GetFormLayoutFilePath("L_via_define")));
        }

        [Fact]
        [DisplayName("SaveLanguage 應寫入以 Namespace 命名的 Language xml")]
        public void SaveLanguage_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };

            access.SaveLanguage(resource);

            Assert.True(File.Exists(temp.Options.GetLanguageFilePath("zh-TW", "Common")));
        }

        [Fact]
        [DisplayName("SaveLanguage 傳入 null 應拋 ArgumentNullException")]
        public void SaveLanguage_NullResource_ThrowsArgumentNullException()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);

            Assert.Throws<ArgumentNullException>(() => access.SaveLanguage(null!));
        }

        [Fact]
        [DisplayName("SaveDefine(Language) 應委派至 SaveLanguage 寫入 Language xml")]
        public void SaveDefine_Language_DelegatesToSaveLanguage()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var resource = new LanguageResource { Lang = "en-US", Namespace = "Messages" };

            access.SaveDefine(DefineType.Language, resource);

            Assert.True(File.Exists(temp.Options.GetLanguageFilePath("en-US", "Messages")));
        }

        [Fact]
        [DisplayName("GetDefine(ProgramSettings) 應回傳 ProgramSettings 實例")]
        public void GetDefine_ProgramSettings_ReturnsProgramSettings()
        {
            using var temp = TempDir.Create();
            XmlCodec.SerializeToFile(new ProgramSettings(), temp.Options.GetProgramSettingsFilePath());
            var access = CreateAccess(temp.Options);

            var result = access.GetDefine(DefineType.ProgramSettings);

            Assert.IsType<ProgramSettings>(result);
        }

        [Fact]
        [DisplayName("GetDefine(FormLayout) 帶正確 key 應回傳 FormLayout 實例")]
        public void GetDefine_FormLayout_WithCorrectKey_ReturnsFormLayout()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            access.SaveFormLayout(new FormLayout { LayoutId = "L_GetTest" });

            var result = access.GetDefine(DefineType.FormLayout, s_formLayoutKey);

            Assert.IsType<FormLayout>(result);
        }

        [Fact]
        [DisplayName("GetDefine(Language) 帶兩個正確 keys 應回傳 LanguageResource 實例")]
        public void GetDefine_Language_WithCorrectKeys_ReturnsLanguageResource()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            access.SaveLanguage(new LanguageResource { Lang = "zh-TW", Namespace = "Common" });

            var result = access.GetDefine(DefineType.Language, s_languageKeys);

            Assert.IsType<LanguageResource>(result);
        }

        [Fact]
        [DisplayName("GetProgramSettings 於檔案存在時應回傳 ProgramSettings 實例")]
        public void GetProgramSettings_FileExists_ReturnsInstance()
        {
            using var temp = TempDir.Create();
            XmlCodec.SerializeToFile(new ProgramSettings(), temp.Options.GetProgramSettingsFilePath());
            var access = CreateAccess(temp.Options);

            var result = access.GetProgramSettings();

            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("GetFormLayout 於 FormLayout 檔案存在時應回傳 FormLayout 實例")]
        public void GetFormLayout_FileExists_ReturnsFormLayout()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            access.SaveFormLayout(new FormLayout { LayoutId = "L_Exist" });

            var result = access.GetFormLayout("L_Exist");

            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("GetLanguage 於 Language 檔案存在時應回傳 LanguageResource 實例")]
        public void GetLanguage_FileExists_ReturnsLanguageResource()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            access.SaveLanguage(new LanguageResource { Lang = "zh-TW", Namespace = "System" });

            var result = access.GetLanguage("zh-TW", "System");

            Assert.NotNull(result);
        }

        private sealed class TempDir : IDisposable
        {
            public string Path { get; }
            public PathOptions Options { get; }

            private TempDir(string path)
            {
                Path = path;
                Options = new PathOptions { DefinePath = path };
            }

            public static TempDir Create()
            {
                var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bee-save-{Guid.NewGuid():N}");
                Directory.CreateDirectory(dir);
                return new TempDir(dir);
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Path))
                        Directory.Delete(Path, recursive: true);
                }
                catch (IOException)
                {
                    // best effort
                }
            }
        }
    }
}
