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
    /// <see cref="LanguageService"/> 行為測試：key 解析、命中、預設語系 fallback、終極回 key。
    /// 使用記憶體 stub <see cref="IDefineAccess"/>，不需檔案系統。
    /// </summary>
    public class LanguageServiceTests
    {
        [Fact]
        [DisplayName("GetLangText 命中當前語系應回傳對應 value")]
        public void GetLangText_HitInRequestedLang_ReturnsValue()
        {
            var defineAccess = new StubDefineAccess("en-US");
            defineAccess.AddResource("zh-TW", "Common", ("OK", "確定"));
            var svc = new LanguageService(defineAccess);

            Assert.Equal("確定", svc.GetLangText("zh-TW", "Common.OK"));
        }

        [Fact]
        [DisplayName("GetLangText 切第一個 '.' 拆 namespace 與 subKey")]
        public void GetLangText_SplitsOnFirstDot()
        {
            var defineAccess = new StubDefineAccess("en-US");
            // subKey 內含 '.'：必須以第一個 '.' 為界
            defineAccess.AddResource("zh-TW", "Customer", ("Field.Name.Caption", "客戶名稱"));
            var svc = new LanguageService(defineAccess);

            Assert.Equal("客戶名稱", svc.GetLangText("zh-TW", "Customer.Field.Name.Caption"));
        }

        [Fact]
        [DisplayName("GetLangText 缺譯時回退預設語系")]
        public void GetLangText_FallsBackToDefaultLang()
        {
            var defineAccess = new StubDefineAccess("en-US");
            // zh-TW 缺譯、en-US 有
            defineAccess.AddResource("en-US", "Common", ("OK", "OK"));
            var svc = new LanguageService(defineAccess);

            Assert.Equal("OK", svc.GetLangText("zh-TW", "Common.OK"));
        }

        [Fact]
        [DisplayName("GetLangText 預設語系也缺譯時回傳 fullKey")]
        public void GetLangText_BothLanguagesMiss_ReturnsFullKey()
        {
            var defineAccess = new StubDefineAccess("en-US");
            // 兩個語系都沒這個 key
            var svc = new LanguageService(defineAccess);

            Assert.Equal("Common.OK", svc.GetLangText("zh-TW", "Common.OK"));
        }

        [Fact]
        [DisplayName("GetLangText 顯式 namespace + subKey 與 fullKey 形式結果一致")]
        public void GetLangText_ExplicitNamespace_MatchesFullKey()
        {
            var defineAccess = new StubDefineAccess("en-US");
            defineAccess.AddResource("zh-TW", "Customer", ("Field.Name.Caption", "客戶名稱"));
            var svc = new LanguageService(defineAccess);

            string viaFullKey = svc.GetLangText("zh-TW", "Customer.Field.Name.Caption");
            string viaExplicit = svc.GetLangText("zh-TW", "Customer", "Field.Name.Caption");

            Assert.Equal(viaFullKey, viaExplicit);
            Assert.Equal("客戶名稱", viaExplicit);
        }

        [Fact]
        [DisplayName("TryGetLangText 命中回傳 true 與 value")]
        public void TryGetLangText_Hit_ReturnsTrue()
        {
            var defineAccess = new StubDefineAccess("en-US");
            defineAccess.AddResource("zh-TW", "Common", ("OK", "確定"));
            var svc = new LanguageService(defineAccess);

            bool ok = svc.TryGetLangText("zh-TW", "Common.OK", out string text);

            Assert.True(ok);
            Assert.Equal("確定", text);
        }

        [Fact]
        [DisplayName("TryGetLangText miss 回傳 false 與 empty 字串（不會 fallback）")]
        public void TryGetLangText_Miss_ReturnsFalseAndEmpty_NoFallback()
        {
            var defineAccess = new StubDefineAccess("en-US");
            defineAccess.AddResource("en-US", "Common", ("OK", "OK")); // 只 en-US 有
            var svc = new LanguageService(defineAccess);

            bool ok = svc.TryGetLangText("zh-TW", "Common.OK", out string text);

            Assert.False(ok);
            Assert.Equal(string.Empty, text);
            // 確認 TryGetLangText 不做預設語系 fallback；fallback 是 GetLangText 的責任
        }

        [Fact]
        [DisplayName("GetLangText 對不存在的 namespace 不丟例外，回傳 fullKey")]
        public void GetLangText_MissingNamespace_ReturnsFullKey()
        {
            var defineAccess = new StubDefineAccess("en-US"); // 沒有任何 resource
            var svc = new LanguageService(defineAccess);

            Assert.Equal("Nonexistent.Foo", svc.GetLangText("zh-TW", "Nonexistent.Foo"));
        }

        [Fact]
        [DisplayName("GetLangText 預設語系 == 當前語系時不重複查詢")]
        public void GetLangText_LangEqualsDefault_NoDoubleLookup()
        {
            var defineAccess = new StubDefineAccess("zh-TW"); // 預設就是 zh-TW
            defineAccess.AddResource("zh-TW", "Common", ("OK", "確定"));
            var svc = new LanguageService(defineAccess);

            Assert.Equal("確定", svc.GetLangText("zh-TW", "Common.OK"));
            // StubDefineAccess.GetLanguageCallCount 計次驗證
            Assert.Equal(1, defineAccess.GetLanguageCallCount);
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

            public int GetLanguageCallCount { get; private set; }

            public void AddResource(string lang, string ns, params (string Key, string Value)[] items)
            {
                var resource = new LanguageResource { Namespace = ns, Lang = lang };
                foreach (var (key, value) in items)
                    resource.Items.Add(key, value);
                _resources[$"{lang}.{ns}"] = resource;
            }

            public LanguageResource GetLanguage(string lang, string ns)
            {
                GetLanguageCallCount++;
                return _resources.TryGetValue($"{lang}.{ns}", out var r) ? r : null!;
            }

            public SystemSettings GetSystemSettings() => _systemSettings;

            // Members we don't exercise in these tests:
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
