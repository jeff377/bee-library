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
    /// 補強 <see cref="LanguageService"/> 邊界路徑的測試覆蓋率：
    /// 建構子 null 防護、GetLangEnum 最終 return null 路徑（lang == defaultLang 或 defaultLang 為空）、
    /// GetLangEnumText 當 langEnum 為 null 的空值傳播路徑、
    /// TryGetLangText 當 resource 存在但 subKey 不存在的分支。
    /// </summary>
    public class LanguageServiceAdditionalTests
    {
        [Fact]
        [DisplayName("LanguageService 建構子傳入 null 應拋 ArgumentNullException")]
        public void Constructor_NullDefineAccess_ThrowsArgumentNullException()
        {
            var exception = Record.Exception(() => new LanguageService(null!));
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        [DisplayName("GetLangEnum 當 lang == defaultLang 且 Enum 不存在時應回傳 null，不嘗試 fallback")]
        public void GetLangEnum_LangEqualsDefaultLang_EnumMiss_ReturnsNull()
        {
            var defineAccess = new MinimalLangDefineAccess("zh-TW");
            var svc = new LanguageService(defineAccess);

            Assert.Null(svc.GetLangEnum("zh-TW", "Common", "Gender"));
        }

        [Fact]
        [DisplayName("GetLangEnum 當 defaultLang 為空字串且 Enum 不存在時應回傳 null")]
        public void GetLangEnum_EmptyDefaultLang_EnumMiss_ReturnsNull()
        {
            var defineAccess = new MinimalLangDefineAccess("");
            var svc = new LanguageService(defineAccess);

            Assert.Null(svc.GetLangEnum("zh-TW", "Common", "Gender"));
        }

        [Fact]
        [DisplayName("GetLangEnumText 當 GetLangEnum 回傳 null 時應回傳 null（空值傳播）")]
        public void GetLangEnumText_NullLangEnum_ReturnsNull()
        {
            var defineAccess = new MinimalLangDefineAccess("en-US");
            var svc = new LanguageService(defineAccess);

            Assert.Null(svc.GetLangEnumText("zh-TW", "Common.NonExistentEnum", "M"));
        }

        [Fact]
        [DisplayName("TryGetLangText 當 resource 存在但 subKey 不在 Items 時應回傳 false 與空字串")]
        public void TryGetLangText_ResourceExistsButKeyMissing_ReturnsFalseAndEmpty()
        {
            var defineAccess = new MinimalLangDefineAccess("en-US");
            defineAccess.AddResource("zh-TW", "Common", ("OK", "確定"));
            var svc = new LanguageService(defineAccess);

            bool result = svc.TryGetLangText("zh-TW", "Common", "Missing", out string text);

            Assert.False(result);
            Assert.Equal(string.Empty, text);
        }

        [Fact]
        [DisplayName("GetLangText 當 defaultLang 為空且主語系未命中時應回傳 namespace.subKey 格式")]
        public void GetLangText_EmptyDefaultLang_PrimaryMiss_ReturnsFallbackKey()
        {
            var defineAccess = new MinimalLangDefineAccess("");
            var svc = new LanguageService(defineAccess);

            Assert.Equal("Common.Missing", svc.GetLangText("zh-TW", "Common", "Missing"));
        }

        private sealed class MinimalLangDefineAccess : IDefineAccess
        {
            private readonly Dictionary<string, LanguageResource> _resources = [];
            private readonly SystemSettings _systemSettings;

            public MinimalLangDefineAccess(string defaultLang)
            {
                _systemSettings = new SystemSettings();
                _systemSettings.CommonConfiguration.DefaultLang = defaultLang;
            }

            public void AddResource(string lang, string ns, params (string Key, string Value)[] items)
            {
                var resource = new LanguageResource { Namespace = ns, Lang = lang };
                foreach (var (key, value) in items)
                    resource.Items.Add(key, value);
                _resources[$"{lang}.{ns}"] = resource;
            }

            public LanguageResource GetLanguage(string lang, string ns)
                => _resources.TryGetValue($"{lang}.{ns}", out var r) ? r : null!;

            public SystemSettings GetSystemSettings() => _systemSettings;

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
