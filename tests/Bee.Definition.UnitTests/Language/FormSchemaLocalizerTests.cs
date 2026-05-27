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
    /// <see cref="FormSchemaLocalizer"/> 行為測試：依約定 key 覆蓋 Caption / DisplayName、
    /// 缺譯保留原值、namespace = ProgId。
    /// </summary>
    public class FormSchemaLocalizerTests
    {
        [Fact]
        [DisplayName("命中時應以語系資源覆蓋 FormSchema.DisplayName")]
        public void Localize_HitSchemaDisplayName_OverridesValue()
        {
            var defineAccess = BuildDefineAccessWith(
                lang: "zh-TW",
                ns: "Customer",
                items: new[]
                {
                    (FormSchemaLocalizer.SchemaDisplayNameKey, "客戶"),
                });
            var schema = BuildSchema("Customer", "Customer (raw)");
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess));

            localizer.Localize(schema, "zh-TW");

            Assert.Equal("客戶", schema.DisplayName);
        }

        [Fact]
        [DisplayName("命中時應以語系資源覆蓋 FormTable.DisplayName（key 帶 TableName）")]
        public void Localize_HitTableDisplayName_OverridesValue()
        {
            var defineAccess = BuildDefineAccessWith(
                lang: "zh-TW",
                ns: "Customer",
                items: new[]
                {
                    (string.Format(CultureInfo.InvariantCulture, FormSchemaLocalizer.TableDisplayNameKeyFormat, "Customer"), "客戶資料"),
                });
            var schema = BuildSchema("Customer", "Customer (raw)");
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess));

            localizer.Localize(schema, "zh-TW");

            Assert.Equal("客戶資料", schema.Tables![0].DisplayName);
        }

        [Fact]
        [DisplayName("命中時應以語系資源覆蓋 FormField.Caption（key 帶 FieldName）")]
        public void Localize_HitFieldCaption_OverridesValue()
        {
            var defineAccess = BuildDefineAccessWith(
                lang: "zh-TW",
                ns: "Customer",
                items: new[]
                {
                    (string.Format(CultureInfo.InvariantCulture, FormSchemaLocalizer.FieldCaptionKeyFormat, "sys_name"), "客戶名稱"),
                });
            var schema = BuildSchema("Customer", "Customer (raw)");
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess));

            localizer.Localize(schema, "zh-TW");

            var nameField = schema.Tables![0].Fields!["sys_name"];
            Assert.Equal("客戶名稱", nameField.Caption);
        }

        [Fact]
        [DisplayName("缺譯時應保留原字面值（向下相容）")]
        public void Localize_MissingKeys_PreservesOriginalValues()
        {
            var defineAccess = BuildDefineAccessWith(lang: "zh-TW", ns: "Customer"); // 空 resource
            var schema = BuildSchema("Customer", "Customer (raw)");
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess));

            localizer.Localize(schema, "zh-TW");

            Assert.Equal("Customer (raw)", schema.DisplayName);
            Assert.Equal("Customer (raw table)", schema.Tables![0].DisplayName);
            Assert.Equal("Customer ID (raw)", schema.Tables![0].Fields!["sys_id"].Caption);
        }

        [Fact]
        [DisplayName("ProgId 為空時應 no-op，不丟例外")]
        public void Localize_EmptyProgId_NoOp()
        {
            var defineAccess = BuildDefineAccessWith(lang: "zh-TW", ns: "Customer");
            var schema = BuildSchema(progId: "", displayName: "X"); // 空 ProgId
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess));

            var exception = Record.Exception(() => localizer.Localize(schema, "zh-TW"));

            Assert.Null(exception);
            Assert.Equal("X", schema.DisplayName); // 未變
        }

        [Fact]
        [DisplayName("Lang 為空或空白時應 no-op，不丟例外")]
        public void Localize_EmptyLang_NoOp()
        {
            var defineAccess = BuildDefineAccessWith(
                lang: "zh-TW",
                ns: "Customer",
                items: new[] { (FormSchemaLocalizer.SchemaDisplayNameKey, "客戶") });
            var schema = BuildSchema("Customer", "Customer (raw)");
            var localizer = new FormSchemaLocalizer(new LanguageService(defineAccess));

            localizer.Localize(schema, "  ");

            Assert.Equal("Customer (raw)", schema.DisplayName);
        }

        private static FormSchema BuildSchema(string progId, string displayName)
        {
            var schema = new FormSchema(progId, displayName) { CategoryId = "common" };
            var table = schema.Tables!.Add("Customer", "Customer (raw table)");
            table.DbTableName = "ft_customer";
            table.Fields!.Add("sys_id", "Customer ID (raw)", FieldDbType.String);
            table.Fields!.Add("sys_name", "Customer Name (raw)", FieldDbType.String);
            return schema;
        }

        private static StubDefineAccess BuildDefineAccessWith(
            string lang, string ns, (string Key, string Value)[]? items = null)
        {
            var stub = new StubDefineAccess(defaultLang: "en-US");
            if (items != null)
                stub.AddResource(lang, ns, items);
            return stub;
        }

        // Minimal IDefineAccess stub — only GetLanguage / GetSystemSettings are exercised.
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
