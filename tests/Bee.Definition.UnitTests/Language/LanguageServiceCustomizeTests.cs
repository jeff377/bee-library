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
        [DisplayName("同 namespace 同時載入套裝與客製：20 key 只客製 5 個，其餘延用套裝，客製獨有 key 亦生效")]
        public void TryGetLangText_PartialOverride_MergesPerKeyAtLookupTime()
        {
            // 套裝語系檔：20 個 key。
            var baseItems = Enumerable.Range(1, 20)
                .Select(i => ($"Key{i:00}", $"套裝{i:00}"))
                .ToArray();
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Customer", baseItems);

            // 客製語系檔：只改其中 5 個 key，另加 1 個套裝沒有的 key。
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "Customer",
                ("Key03", "客製03"), ("Key07", "客製07"), ("Key11", "客製11"),
                ("Key15", "客製15"), ("Key20", "客製20"),
                ("KeyOnlyInCustomize", "客製獨有"));
            var svc = new LanguageService(defineAccess, reader);

            var overridden = new[] { "Key03", "Key07", "Key11", "Key15", "Key20" };
            foreach (var (key, _) in baseItems)
            {
                string expected = overridden.Contains(key, StringComparer.Ordinal)
                    ? $"客製{key.Substring(3)}"   // 同一個 key 兩邊都有 → 客製優先
                    : $"套裝{key.Substring(3)}";  // 客製沒有 → 延用套裝
                Assert.Equal(expected, svc.GetLangText("acme", "zh-TW", "Customer", key));
            }

            // 客製獨有的 key 也查得到（套裝無則加入）。
            Assert.Equal("客製獨有", svc.GetLangText("acme", "zh-TW", "Customer", "KeyOnlyInCustomize"));

            // 同一份套裝資源在未帶 customizeId 時完全不受影響（另一家公司 / 未客製的公司）。
            Assert.Equal("套裝03", svc.GetLangText("zh-TW", "Customer", "Key03"));
        }

        [Fact]
        [DisplayName("跨租戶隔離：A 公司的客製不影響 B 公司的查找結果")]
        public void TryGetLangText_DifferentCustomizeIds_AreIsolated()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Customer", ("Key01", "套裝01"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "Customer", ("Key01", "acme 客製"));
            reader.AddLanguage("globex", "zh-TW", "Customer", ("Key01", "globex 客製"));
            var svc = new LanguageService(defineAccess, reader);

            Assert.Equal("acme 客製", svc.GetLangText("acme", "zh-TW", "Customer", "Key01"));
            Assert.Equal("globex 客製", svc.GetLangText("globex", "zh-TW", "Customer", "Key01"));
            // 沒有客製檔的公司照樣拿套裝值。
            Assert.Equal("套裝01", svc.GetLangText("initech", "zh-TW", "Customer", "Key01"));
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
