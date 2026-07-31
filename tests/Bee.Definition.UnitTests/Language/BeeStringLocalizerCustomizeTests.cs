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
    /// <see cref="BeeStringLocalizer{T}"/> 租戶客製化管道測試：customizeIdProvider 命中→cust 值；
    /// cust 缺 key→base 值；既有 1-arg / 2-arg 建構子→短路純 base（reader 零呼叫，逐位元同現況）。
    /// </summary>
    public class BeeStringLocalizerCustomizeTests
    {
        // Marker type whose name maps to the "CommonResources" language namespace.
        // Avoids the BCL "Common" / System.Data.Common collision flagged by CA1724.
        public sealed class CommonResources { }

        [Fact]
        [DisplayName("customizeIdProvider 有值且 cust 有 key 時應回 cust 值")]
        public void Indexer_CustHasKey_ReturnsCustValue()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "CommonResources", ("OK", "確定"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "CommonResources", ("OK", "送出"));
            var svc = new LanguageService(defineAccess, reader);
            var localizer = new BeeStringLocalizer<CommonResources>(svc, () => "zh-TW", () => "acme");

            var result = localizer["OK"];

            Assert.Equal("送出", result.Value);
            Assert.False(result.ResourceNotFound);
        }

        [Fact]
        [DisplayName("cust 缺該 key 時應回退 base 值")]
        public void Indexer_CustMissesKey_ReturnsBaseValue()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "CommonResources", ("OK", "確定"), ("Cancel", "取消"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "CommonResources", ("OK", "送出"));
            var svc = new LanguageService(defineAccess, reader);
            var localizer = new BeeStringLocalizer<CommonResources>(svc, () => "zh-TW", () => "acme");

            Assert.Equal("取消", localizer["Cancel"].Value);
        }

        [Fact]
        [DisplayName("cust 與 base 都缺 key 時 ResourceNotFound=true 且值為 fullKey")]
        public void Indexer_BothMiss_ReturnsResourceNotFound()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            var reader = new SpyCustomizeReader();
            var svc = new LanguageService(defineAccess, reader);
            var localizer = new BeeStringLocalizer<CommonResources>(svc, () => "zh-TW", () => "acme");

            var result = localizer["Nonexistent"];

            Assert.Equal("CommonResources.Nonexistent", result.Value);
            Assert.True(result.ResourceNotFound);
        }

        [Fact]
        [DisplayName("customizeIdProvider 回傳 null 時視為空字串，走純 base 不拋例外")]
        public void Indexer_NullCustomizeId_TreatedAsEmpty()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "CommonResources", ("OK", "確定"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "CommonResources", ("OK", "送出"));
            var svc = new LanguageService(defineAccess, reader);
            var localizer = new BeeStringLocalizer<CommonResources>(svc, () => "zh-TW", () => null!);

            Assert.Equal("確定", localizer["OK"].Value);
            Assert.Equal(0, reader.GetCustomizeLanguageCallCount);
        }

        [Fact]
        [DisplayName("customizeIdProvider 傳 null 應拋 ArgumentNullException")]
        public void Ctor_NullCustomizeIdProvider_Throws()
        {
            var svc = new LanguageService(new StubDefineAccess("zh-TW"));

            Assert.Throws<ArgumentNullException>(() =>
                new BeeStringLocalizer<CommonResources>(svc, () => "zh-TW", null!));
        }

        // ---- 回歸防護：未設 CustomizeId 的部署行為必須與現況逐位元一致 ----

        [Fact]
        [DisplayName("回歸防護：2-arg 建構子不得碰客製層（reader 零呼叫）")]
        public void Indexer_LangProviderOnlyCtor_NeverTouchesCustomizeLayer()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "CommonResources", ("OK", "確定"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "CommonResources", ("OK", "送出"));
            var svc = new LanguageService(defineAccess, reader);
            var localizer = new BeeStringLocalizer<CommonResources>(svc, () => "zh-TW");

            Assert.Equal("確定", localizer["OK"].Value);
            Assert.Equal(0, reader.GetCustomizeLanguageCallCount);
        }

        [Fact]
        [DisplayName("回歸防護：customizeIdProvider 回傳空字串等同 2-arg 建構子")]
        public void Indexer_EmptyCustomizeId_MatchesLangProviderOnlyCtor()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "CommonResources", ("OK", "確定"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "CommonResources", ("OK", "送出"));
            var svc = new LanguageService(defineAccess, reader);

            var legacy = new BeeStringLocalizer<CommonResources>(svc, () => "zh-TW")["OK"];
            var explicitEmpty = new BeeStringLocalizer<CommonResources>(svc, () => "zh-TW", () => string.Empty)["OK"];

            Assert.Equal(legacy.Value, explicitEmpty.Value);
            Assert.Equal(legacy.ResourceNotFound, explicitEmpty.ResourceNotFound);
            Assert.Equal(0, reader.GetCustomizeLanguageCallCount);
        }

        // ---- Test doubles ----

        private sealed class SpyCustomizeReader : ICustomizeDefineReader
        {
            private readonly Dictionary<string, LanguageResource> _languages = [];

            public int GetCustomizeLanguageCallCount { get; private set; }

            public void AddLanguage(string customizeId, string lang, string ns, params (string Key, string Value)[] items)
            {
                var resource = new LanguageResource { Namespace = ns, Lang = lang };
                foreach (var (key, value) in items)
                    resource.Items.Add(key, value);
                _languages[$"{customizeId}.{lang}.{ns}"] = resource;
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
