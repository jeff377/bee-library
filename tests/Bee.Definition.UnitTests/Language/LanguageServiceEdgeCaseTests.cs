using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests.Language
{
    /// <summary>
    /// 補強 <see cref="LanguageService"/> 邊界路徑：
    /// <c>SplitFullKey</c> 無 '.' 分支，以及 <see cref="LanguageService.GetLangEnum(string,string,string)"/>
    /// 的空白 namespace / enumName 守衛（line 89）。
    /// </summary>
    public class LanguageServiceEdgeCaseTests
    {
        [Fact]
        [DisplayName("GetLangText fullKey 無 '.' 時應以整個 key 作 namespace，subKey 為空字串，最終回傳 'key.'")]
        public void GetLangText_KeyWithoutDot_ReturnsKeyWithTrailingDot()
        {
            var defineAccess = new MinimalDefineAccess("en-US");
            var svc = new LanguageService(defineAccess);
            Assert.Equal("NoDot.", svc.GetLangText("zh-TW", "NoDot"));
        }

        [Fact]
        [DisplayName("GetLangEnum fullName 無 '.' 時 enumName 為空字串，應回傳 null")]
        public void GetLangEnum_FullNameWithoutDot_ReturnsNull()
        {
            var defineAccess = new MinimalDefineAccess("en-US");
            var svc = new LanguageService(defineAccess);
            Assert.Null(svc.GetLangEnum("zh-TW", "GenderEnum"));
        }

        [Fact]
        [DisplayName("GetLangEnum namespace 為空字串時應立即回傳 null")]
        public void GetLangEnum_BlankNamespace_ReturnsNull()
        {
            var defineAccess = new MinimalDefineAccess("en-US");
            var svc = new LanguageService(defineAccess);
            Assert.Null(svc.GetLangEnum("zh-TW", "", "Gender"));
        }

        [Fact]
        [DisplayName("GetLangEnum enumName 為空字串時應立即回傳 null")]
        public void GetLangEnum_BlankEnumName_ReturnsNull()
        {
            var defineAccess = new MinimalDefineAccess("en-US");
            var svc = new LanguageService(defineAccess);
            Assert.Null(svc.GetLangEnum("zh-TW", "Common", ""));
        }

        // ──────────────────────────────────────────────────────────────
        // Minimal stub — implements only GetLanguage and GetSystemSettings;
        // all other members throw NotImplementedException because these
        // tests never invoke the corresponding code paths.
        // ──────────────────────────────────────────────────────────────
        private sealed class MinimalDefineAccess : IDefineAccess
        {
            private readonly SystemSettings _settings;

            public MinimalDefineAccess(string defaultLang)
            {
                _settings = new SystemSettings();
                _settings.CommonConfiguration.DefaultLang = defaultLang;
            }

            public LanguageResource GetLanguage(string lang, string ns) => null!;
            public SystemSettings GetSystemSettings() => _settings;

            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
            public DatabaseSettings GetDatabaseSettings() => throw new NotImplementedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotImplementedException();
            public ProgramSettings GetProgramSettings() => throw new NotImplementedException();
            public void SaveProgramSettings(ProgramSettings settings) => throw new NotImplementedException();
            public DbCategorySettings GetDbCategorySettings() => throw new NotImplementedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotImplementedException();
            public TableSchema GetTableSchema(string categoryId, string tableName) => throw new NotImplementedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotImplementedException();
            public FormSchema GetFormSchema(string progId) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public FormLayout GetFormLayout(string layoutId) => throw new NotImplementedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
            public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
        }
    }
}
