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
    /// <see cref="LanguageService"/> 租戶客製化疊加測試：cust 有 key→cust 值；cust 無 key→base 值；
    /// cust resource 不存在→全 base；customizeId 空 / 無 reader→短路純 base（reader 零呼叫）。
    /// </summary>
    public class LanguageServiceCustomizeTests
    {
        [Fact]
        [DisplayName("cust 有 key 時應回傳 cust 值（覆寫 base）")]
        public void TryGetLangText_CustHasKey_ReturnsCustValue()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Common", ("OK", "確定"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "Common", ("OK", "客製確定"));
            var svc = new LanguageService(defineAccess, reader);

            Assert.Equal("客製確定", svc.GetLangText("acme", "zh-TW", "Common", "OK"));
        }

        [Fact]
        [DisplayName("cust resource 有但缺該 key 時應回退 base 值")]
        public void TryGetLangText_CustMissesKey_ReturnsBaseValue()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Common", ("OK", "確定"), ("Cancel", "取消"));
            var reader = new SpyCustomizeReader();
            // cust resource 只覆寫 OK，沒有 Cancel
            reader.AddLanguage("acme", "zh-TW", "Common", ("OK", "客製確定"));
            var svc = new LanguageService(defineAccess, reader);

            Assert.Equal("取消", svc.GetLangText("acme", "zh-TW", "Common", "Cancel"));
        }

        [Fact]
        [DisplayName("cust resource 不存在時應全回 base 值")]
        public void TryGetLangText_NoCustResource_ReturnsBaseValue()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Common", ("OK", "確定"));
            var reader = new SpyCustomizeReader(); // acme 沒有任何客製
            var svc = new LanguageService(defineAccess, reader);

            Assert.Equal("確定", svc.GetLangText("acme", "zh-TW", "Common", "OK"));
        }

        [Fact]
        [DisplayName("customizeId 空時短路純 base，reader 零呼叫")]
        public void EmptyCustomizeId_ShortCircuits_ReaderNotCalled()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Common", ("OK", "確定"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "Common", ("OK", "客製確定"));
            var svc = new LanguageService(defineAccess, reader);

            // 經由不帶 customizeId 的 base 多載
            Assert.Equal("確定", svc.GetLangText("zh-TW", "Common.OK"));
            Assert.Equal(0, reader.GetCustomizeLanguageCallCount);
        }

        [Fact]
        [DisplayName("無 reader 注入時行為與純 base 一致（向後相容）")]
        public void NoReader_BehavesAsBase()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Common", ("OK", "確定"));
            var svc = new LanguageService(defineAccess); // 無 reader

            // 即使帶 customizeId，無 reader 即退化為 base
            Assert.Equal("確定", svc.GetLangText("acme", "zh-TW", "Common", "OK"));
        }

        [Fact]
        [DisplayName("Enum 疊加：cust 有同名 enum 時回 cust enum")]
        public void GetLangEnum_CustHasEnum_ReturnsCustEnum()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddEnum("zh-TW", "Common", "Gender", ("M", "男"), ("F", "女"));
            var reader = new SpyCustomizeReader();
            reader.AddEnum("acme", "zh-TW", "Common", "Gender", ("M", "先生"), ("F", "小姐"));
            var svc = new LanguageService(defineAccess, reader);

            var result = svc.GetLangEnum("acme", "zh-TW", "Common", "Gender");

            Assert.NotNull(result);
            Assert.Equal("先生", result!.GetText("M"));
        }

        [Fact]
        [DisplayName("Enum 疊加：cust 無該 enum 時回 base enum")]
        public void GetLangEnum_CustMissesEnum_ReturnsBaseEnum()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddEnum("zh-TW", "Common", "Gender", ("M", "男"), ("F", "女"));
            var reader = new SpyCustomizeReader();
            // cust resource 存在但只覆寫文字 key、沒有 Gender enum
            reader.AddLanguage("acme", "zh-TW", "Common", ("OK", "客製確定"));
            var svc = new LanguageService(defineAccess, reader);

            var result = svc.GetLangEnum("acme", "zh-TW", "Common", "Gender");

            Assert.NotNull(result);
            Assert.Equal("男", result!.GetText("M"));
        }

        // ---- Test doubles ----

        private sealed class SpyCustomizeReader : ICustomizeDefineReader
        {
            private readonly Dictionary<string, LanguageResource> _languages = [];

            public int GetCustomizeLanguageCallCount { get; private set; }

            public void AddLanguage(string customizeId, string lang, string ns, params (string Key, string Value)[] items)
            {
                var resource = GetOrCreate(customizeId, lang, ns);
                foreach (var (key, value) in items)
                    resource.Items.Add(key, value);
            }

            public void AddEnum(string customizeId, string lang, string ns, string enumName, params (string Code, string Text)[] entries)
            {
                var resource = GetOrCreate(customizeId, lang, ns);
                var langEnum = new LanguageEnum { Name = enumName };
                foreach (var (code, text) in entries)
                    langEnum.Entries.Add(code, text);
                resource.Enums.Add(langEnum);
            }

            private LanguageResource GetOrCreate(string customizeId, string lang, string ns)
            {
                string key = $"{customizeId}.{lang}.{ns}";
                if (!_languages.TryGetValue(key, out var resource))
                {
                    resource = new LanguageResource { Namespace = ns, Lang = lang };
                    _languages[key] = resource;
                }
                return resource;
            }

            public LanguageResource? GetCustomizeLanguage(string customizeId, string lang, string ns)
            {
                GetCustomizeLanguageCallCount++;
                return _languages.TryGetValue($"{customizeId}.{lang}.{ns}", out var r) ? r : null;
            }

            public ProgramSettings? GetCustomizeProgramSettings(string customizeId) => null;
            public FormLayout? GetCustomizeFormLayout(string customizeId, string layoutId) => null;
        }

        private sealed class StubDefineAccess : IDefineAccess
        {
            private readonly Dictionary<string, LanguageResource> _resources = [];
            private readonly SystemSettings _systemSettings;

            public StubDefineAccess(string defaultLang)
            {
                _systemSettings = new SystemSettings();
                _systemSettings.CommonConfiguration.DefaultLang = defaultLang;
            }

            public void AddResource(string lang, string ns, params (string Key, string Value)[] items)
            {
                var resource = GetOrCreate(lang, ns);
                foreach (var (key, value) in items)
                    resource.Items.Add(key, value);
            }

            public void AddEnum(string lang, string ns, string enumName, params (string Code, string Text)[] entries)
            {
                var resource = GetOrCreate(lang, ns);
                var langEnum = new LanguageEnum { Name = enumName };
                foreach (var (code, text) in entries)
                    langEnum.Entries.Add(code, text);
                resource.Enums.Add(langEnum);
            }

            private LanguageResource GetOrCreate(string lang, string ns)
            {
                string key = $"{lang}.{ns}";
                if (!_resources.TryGetValue(key, out var resource))
                {
                    resource = new LanguageResource { Namespace = ns, Lang = lang };
                    _resources[key] = resource;
                }
                return resource;
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
