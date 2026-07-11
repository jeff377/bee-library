using System.ComponentModel;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="ProgramSettingsFormBoTypeResolver"/> 補洞覆蓋測試：建構子 null 防護、
    /// 型別載入成功但型別不存在（GetType→null）回退、客製 ProgramSettings reference 變更觸發 cache reset、
    /// FindItem 跨多個 category 尋找。
    /// </summary>
    public class ProgramSettingsFormBoTypeResolverCoverageTests
    {
        private static string BaseFormBoFqn =>
            $"{typeof(ProgramSettingsFormBoTypeResolverTests.TestableCustomFormBo).FullName}, " +
            $"{typeof(ProgramSettingsFormBoTypeResolverTests.TestableCustomFormBo).Assembly.GetName().Name}";

        private static string TenantFormBoFqn =>
            $"{typeof(ProgramSettingsFormBoTypeResolverCustomizeTests.TenantFormBo).FullName}, " +
            $"{typeof(ProgramSettingsFormBoTypeResolverCustomizeTests.TenantFormBo).Assembly.GetName().Name}";

        private static ProgramSettings BuildSettings(params (string progId, string? businessObject)[] items)
        {
            var settings = new ProgramSettings();
            var category = settings.Categories!.Add("C01", "主檔");
            foreach (var (progId, businessObject) in items)
            {
                var item = category.Items!.Add(progId, progId);
                if (businessObject != null) item.BusinessObject = businessObject;
            }
            return settings;
        }

        // Two categories; the target progId lives only in the second one → exercises FindItem's loop.
        private static ProgramSettings BuildTwoCategorySettings(string progId, string businessObject)
        {
            var settings = new ProgramSettings();
            settings.Categories!.Add("C01", "主檔一"); // 空的第一分類
            var second = settings.Categories!.Add("C02", "主檔二");
            var item = second.Items!.Add(progId, progId);
            item.BusinessObject = businessObject;
            return settings;
        }

        [Fact]
        [DisplayName("建構子 defineAccess 為 null 應丟 ArgumentNullException")]
        public void Ctor_NullDefineAccess_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgramSettingsFormBoTypeResolver(null!));
        }

        [Fact]
        [DisplayName("BusinessObject 型別名組件可載入但型別不存在（GetType→null）時應回退 FormBusinessObject")]
        public void Resolve_TypeNameLoadableAssemblyButMissingType_FallsBack()
        {
            var settings = BuildSettings(("P001", "Bee.Business.NoSuchTypeXyz, Bee.Business"));
            var defineAccess = new MutableDefineAccess(settings);
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess);

            var result = resolver.Resolve("P001");

            Assert.Equal(typeof(FormBusinessObject), result);
        }

        [Fact]
        [DisplayName("客製 ProgramSettings 實例變更（reference 不同）應 reset cache 並重新解析")]
        public void Resolve_CustSettingsInstanceReplaced_ResetsCache()
        {
            var defineAccess = new MutableDefineAccess(BuildSettings(("P001", BaseFormBoFqn)));
            var reader = new MutableCustomizeReader();
            reader.Set("acme", BuildSettings(("P001", TenantFormBoFqn)));
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess, reader);

            var first = resolver.Resolve("acme", "P001");
            Assert.Equal(typeof(ProgramSettingsFormBoTypeResolverCustomizeTests.TenantFormBo), first);

            // File-watcher reload: hand back a new instance that no longer carries P001 at all.
            reader.Set("acme", BuildSettings(("P999", null)));

            var second = resolver.Resolve("acme", "P001");

            // Cust no longer overrides P001 → falls through to the base entry's BO.
            Assert.Equal(typeof(ProgramSettingsFormBoTypeResolverTests.TestableCustomFormBo), second);
        }

        [Fact]
        [DisplayName("FindItem 應跨多個 category 尋找目標 progId")]
        public void Resolve_ProgIdInSecondCategory_Resolves()
        {
            var defineAccess = new MutableDefineAccess(BuildTwoCategorySettings("P002", TenantFormBoFqn));
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess);

            var result = resolver.Resolve("P002");

            Assert.Equal(typeof(ProgramSettingsFormBoTypeResolverCustomizeTests.TenantFormBo), result);
        }

        // ---- Test doubles ----

        private sealed class MutableCustomizeReader : ICustomizeDefineReader
        {
            private readonly Dictionary<string, ProgramSettings> _settings = new(StringComparer.Ordinal);

            public void Set(string customizeId, ProgramSettings settings) => _settings[customizeId] = settings;

            public ProgramSettings? GetCustomizeProgramSettings(string customizeId)
                => _settings.TryGetValue(customizeId, out var s) ? s : null;

            public LanguageResource? GetCustomizeLanguage(string customizeId, string lang, string ns) => null;
            public FormLayout? GetCustomizeFormLayout(string customizeId, string layoutId) => null;
        }

        private sealed class MutableDefineAccess : IDefineAccess
        {
            public ProgramSettings Current { get; set; }
            public MutableDefineAccess(ProgramSettings initial) { Current = initial; }
            public ProgramSettings GetProgramSettings() => Current;

            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public SystemSettings GetSystemSettings() => throw new NotImplementedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
            public DatabaseSettings GetDatabaseSettings() => throw new NotImplementedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotImplementedException();
            public void SaveProgramSettings(ProgramSettings settings) => throw new NotImplementedException();
            public DbCategorySettings GetDbCategorySettings() => throw new NotImplementedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotImplementedException();
            public TableSchema GetTableSchema(string categoryId, string tableName) => throw new NotImplementedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotImplementedException();
            public FormSchema GetFormSchema(string progId) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public FormLayout GetFormLayout(string layoutId) => throw new NotImplementedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
            public LanguageResource GetLanguage(string lang, string ns) => throw new NotImplementedException();
            public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
        }
    }
}
