using System.ComponentModel;
using System.Globalization;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests.Language
{
    /// <summary>
    /// <see cref="FormSchemaLocalizer"/> 租戶客製化疊加測試：cust 有 key→cust 值；cust 無 key→base 值；
    /// enum 疊加；customizeId 空 / 舊 2-arg 多載→短路純 base（reader 零呼叫，逐位元同現況）。
    /// </summary>
    public class FormSchemaLocalizerCustomizeTests
    {
        private static readonly string[] s_statusCodes = ["1", "9"];
        private static readonly string[] s_statusTexts = ["暫停", "客製狀態"];

        private static string FieldKey(string fieldName)
            => string.Format(CultureInfo.InvariantCulture, FormSchemaLocalizer.FieldCaptionKeyFormat, fieldName);

        private static string TableKey(string tableName)
            => string.Format(CultureInfo.InvariantCulture, FormSchemaLocalizer.TableDisplayNameKeyFormat, tableName);

        [Fact]
        [DisplayName("cust 有 Field.Caption 時應以 cust 值覆蓋 base 值")]
        public void Localize_CustHasFieldCaption_UsesCustValue()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Customer", (FieldKey("sys_name"), "客戶名稱"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "Customer", (FieldKey("sys_name"), "客戶抬頭"));
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess, reader));
            var schema = BuildSchema();

            localizer.Localize(schema, "acme", "zh-TW");

            Assert.Equal("客戶抬頭", schema.Tables![0].Fields!["sys_name"].Caption);
        }

        [Fact]
        [DisplayName("cust 缺該 key 時應回退 base 值（per-key 疊加）")]
        public void Localize_CustMissesKey_FallsBackToBase()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Customer",
                (FormSchemaLocalizer.SchemaDisplayNameKey, "客戶"),
                (TableKey("Customer"), "客戶資料"),
                (FieldKey("sys_id"), "客戶編號"),
                (FieldKey("sys_name"), "客戶名稱"));
            var reader = new SpyCustomizeReader();
            // 客製檔只覆寫一個 key，其餘應全部來自 base
            reader.AddLanguage("acme", "zh-TW", "Customer", (FieldKey("sys_name"), "客戶抬頭"));
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess, reader));
            var schema = BuildSchema();

            localizer.Localize(schema, "acme", "zh-TW");

            Assert.Equal("客戶", schema.DisplayName);
            Assert.Equal("客戶資料", schema.Tables![0].DisplayName);
            Assert.Equal("客戶編號", schema.Tables![0].Fields!["sys_id"].Caption);
            Assert.Equal("客戶抬頭", schema.Tables![0].Fields!["sys_name"].Caption);
        }

        [Fact]
        [DisplayName("cust 有同名 LanguageEnum 時 ListItems 應整組換成客製選項集")]
        public void Localize_CustHasLangEnum_ListItemsReplacedByCustEnum()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddEnum("zh-TW", "Customer", "Status", ("0", "啟用"), ("1", "停用"), ("2", "凍結"));
            var reader = new SpyCustomizeReader();
            // 客製檔必須列出它要的完整選項集——沒列的套裝選項不會被併進來
            reader.AddEnum("acme", "zh-TW", "Customer", "Status", ("1", "暫停"), ("9", "客製狀態"));
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess, reader));
            var schema = BuildSchemaWithLangEnumField("Status");

            localizer.Localize(schema, "acme", "zh-TW");

            var statusField = schema.Tables![0].Fields!["status"];
            Assert.Equal(s_statusCodes, statusField.ListItems!.Select(i => i.Value).ToArray());
            Assert.Equal(s_statusTexts, statusField.ListItems!.Select(i => i.Text).ToArray());
        }

        [Fact]
        [DisplayName("cust resource 不存在時所有欄位應全回 base 值")]
        public void Localize_NoCustResource_AllValuesFromBase()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Customer",
                (FormSchemaLocalizer.SchemaDisplayNameKey, "客戶"),
                (FieldKey("sys_name"), "客戶名稱"));
            var reader = new SpyCustomizeReader(); // acme 沒有任何客製檔
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess, reader));
            var schema = BuildSchema();

            localizer.Localize(schema, "acme", "zh-TW");

            Assert.Equal("客戶", schema.DisplayName);
            Assert.Equal("客戶名稱", schema.Tables![0].Fields!["sys_name"].Caption);
        }

        // ---- 回歸防護：未設 CustomizeId 的部署行為必須與現況逐位元一致 ----

        [Fact]
        [DisplayName("回歸防護：舊 2-arg 多載不得碰客製層（reader 零呼叫）")]
        public void Localize_LegacyOverload_NeverTouchesCustomizeLayer()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Customer", (FieldKey("sys_name"), "客戶名稱"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "Customer", (FieldKey("sys_name"), "客戶抬頭"));
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess, reader));
            var schema = BuildSchema();

            localizer.Localize(schema, "zh-TW");

            Assert.Equal("客戶名稱", schema.Tables![0].Fields!["sys_name"].Caption);
            Assert.Equal(0, reader.GetCustomizeLanguageCallCount);
        }

        [Fact]
        [DisplayName("回歸防護：customizeId 為空時不得碰客製層（reader 零呼叫）")]
        public void Localize_EmptyCustomizeId_NeverTouchesCustomizeLayer()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Customer", (FieldKey("sys_name"), "客戶名稱"));
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "Customer", (FieldKey("sys_name"), "客戶抬頭"));
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess, reader));
            var schema = BuildSchema();

            localizer.Localize(schema, string.Empty, "zh-TW");

            Assert.Equal("客戶名稱", schema.Tables![0].Fields!["sys_name"].Caption);
            Assert.Equal(0, reader.GetCustomizeLanguageCallCount);
        }

        [Fact]
        [DisplayName("回歸防護：空 customizeId 的結果與舊 2-arg 多載完全相同")]
        public void Localize_EmptyCustomizeId_MatchesLegacyOverloadResult()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            defineAccess.AddResource("zh-TW", "Customer",
                (FormSchemaLocalizer.SchemaDisplayNameKey, "客戶"),
                (TableKey("Customer"), "客戶資料"),
                (FieldKey("sys_id"), "客戶編號"));
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess));

            var viaLegacy = BuildSchema();
            localizer.Localize(viaLegacy, "zh-TW");
            var viaCustomize = BuildSchema();
            localizer.Localize(viaCustomize, string.Empty, "zh-TW");

            Assert.Equal(viaLegacy.DisplayName, viaCustomize.DisplayName);
            Assert.Equal(viaLegacy.Tables![0].DisplayName, viaCustomize.Tables![0].DisplayName);
            Assert.Equal(viaLegacy.Tables![0].Fields!["sys_id"].Caption, viaCustomize.Tables![0].Fields!["sys_id"].Caption);
            Assert.Equal(viaLegacy.Tables![0].Fields!["sys_name"].Caption, viaCustomize.Tables![0].Fields!["sys_name"].Caption);
        }

        [Fact]
        [DisplayName("Lang 為空時即使帶 customizeId 也應 no-op（短路早於客製查找）")]
        public void Localize_EmptyLang_NoOpEvenWithCustomizeId()
        {
            var defineAccess = new StubDefineAccess("zh-TW");
            var reader = new SpyCustomizeReader();
            reader.AddLanguage("acme", "zh-TW", "Customer", (FieldKey("sys_name"), "客戶抬頭"));
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess, reader));
            var schema = BuildSchema();

            localizer.Localize(schema, "acme", "  ");

            Assert.Equal("Customer Name (raw)", schema.Tables![0].Fields!["sys_name"].Caption);
            Assert.Equal(0, reader.GetCustomizeLanguageCallCount);
        }

        // ---- Fixtures ----

        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Customer", "Customer (raw)") { CategoryId = "common" };
            var table = schema.Tables!.Add("Customer", "Customer (raw table)");
            table.DbTableName = "ft_customer";
            table.Fields!.Add("sys_id", "Customer ID (raw)", FieldDbType.String);
            table.Fields!.Add("sys_name", "Customer Name (raw)", FieldDbType.String);
            return schema;
        }

        private static FormSchema BuildSchemaWithLangEnumField(string langEnumName)
        {
            var schema = new FormSchema("Customer", "Customer") { CategoryId = "common" };
            var table = schema.Tables!.Add("Customer", "Customer");
            table.Fields!.Add(new FormField("status", "Status", FieldDbType.String)
            {
                LangEnumName = langEnumName,
            });
            return schema;
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
            public MenuSettings? GetCustomizeMenuSettings(string customizeId) => null;
            public PluginSettings? GetCustomizePluginSettings(string customizeId) => null;
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
