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
    /// <see cref="ProgramSettingsFormBoTypeResolver"/> 租戶客製化疊加測試：cust 有 progId→客製 BO；
    /// cust 無→base BO；type cache 以 (custCode, progId) 隔離；custCode 空 / 無 reader→短路純 base。
    /// </summary>
    public class ProgramSettingsFormBoTypeResolverCustomizeTests
    {
        public class TenantFormBo : FormBusinessObject
        {
            public TenantFormBo(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
                : base(ctx, accessToken, progId, isLocalCall) { }
        }

        private static string TenantFormBoFqn =>
            $"{typeof(TenantFormBo).FullName}, {typeof(TenantFormBo).Assembly.GetName().Name}";

        private static string BaseFormBoFqn =>
            $"{typeof(ProgramSettingsFormBoTypeResolverTests.TestableCustomFormBo).FullName}, " +
            $"{typeof(ProgramSettingsFormBoTypeResolverTests.TestableCustomFormBo).Assembly.GetName().Name}";

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

        [Fact]
        [DisplayName("cust 有該 progId 時應解析為客製 BO（覆寫 base）")]
        public void Resolve_CustHasProgId_ReturnsCustomizeBo()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("P001", BaseFormBoFqn)));
            var reader = new SpyCustomizeReader();
            reader.SetProgramSettings("acme", BuildSettings(("P001", TenantFormBoFqn)));
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess, reader);

            var result = resolver.Resolve("acme", "P001");

            Assert.Equal(typeof(TenantFormBo), result);
        }

        [Fact]
        [DisplayName("cust 無該 progId 時應回退 base BO")]
        public void Resolve_CustMissesProgId_FallsBackToBase()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("P001", BaseFormBoFqn)));
            var reader = new SpyCustomizeReader();
            // cust settings 只覆寫 P999，沒有 P001
            reader.SetProgramSettings("acme", BuildSettings(("P999", TenantFormBoFqn)));
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess, reader);

            var result = resolver.Resolve("acme", "P001");

            Assert.Equal(typeof(ProgramSettingsFormBoTypeResolverTests.TestableCustomFormBo), result);
        }

        [Fact]
        [DisplayName("type cache 以 (custCode, progId) 隔離：不同租戶同一 progId 解析互不干擾")]
        public void Resolve_DifferentCustCodes_IsolatedCache()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("P001", BaseFormBoFqn)));
            var reader = new SpyCustomizeReader();
            reader.SetProgramSettings("acme", BuildSettings(("P001", TenantFormBoFqn)));
            // globex 沒有客製
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess, reader);

            var acme = resolver.Resolve("acme", "P001");
            var globex = resolver.Resolve("globex", "P001");
            var baseOnly = resolver.Resolve("P001");

            Assert.Equal(typeof(TenantFormBo), acme);
            Assert.Equal(typeof(ProgramSettingsFormBoTypeResolverTests.TestableCustomFormBo), globex);
            Assert.Equal(typeof(ProgramSettingsFormBoTypeResolverTests.TestableCustomFormBo), baseOnly);
        }

        [Fact]
        [DisplayName("custCode 空時短路純 base，reader 零呼叫")]
        public void Resolve_EmptyCustCode_ShortCircuits_ReaderNotCalled()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("P001", BaseFormBoFqn)));
            var reader = new SpyCustomizeReader();
            reader.SetProgramSettings("acme", BuildSettings(("P001", TenantFormBoFqn)));
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess, reader);

            var result = resolver.Resolve("P001");

            Assert.Equal(typeof(ProgramSettingsFormBoTypeResolverTests.TestableCustomFormBo), result);
            Assert.Equal(0, reader.GetCustomizeProgramSettingsCallCount);
        }

        [Fact]
        [DisplayName("無 reader 注入時即使帶 custCode 也走純 base（向後相容）")]
        public void Resolve_NoReader_BehavesAsBase()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("P001", BaseFormBoFqn)));
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess); // 無 reader

            var result = resolver.Resolve("acme", "P001");

            Assert.Equal(typeof(ProgramSettingsFormBoTypeResolverTests.TestableCustomFormBo), result);
        }

        [Fact]
        [DisplayName("base ProgramSettings 缺檔但 cust 有該 progId 時仍解析為客製 BO")]
        public void Resolve_BaseMissingButCustHasProgId_ReturnsCustomizeBo()
        {
            var defineAccess = new ThrowingProgramSettingsDefineAccess();
            var reader = new SpyCustomizeReader();
            reader.SetProgramSettings("acme", BuildSettings(("P001", TenantFormBoFqn)));
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess, reader);

            var result = resolver.Resolve("acme", "P001");

            Assert.Equal(typeof(TenantFormBo), result);
        }

        // ---- Test doubles ----

        private sealed class SpyCustomizeReader : ICustomizeDefineReader
        {
            private readonly Dictionary<string, ProgramSettings> _settings = new(StringComparer.Ordinal);

            public int GetCustomizeProgramSettingsCallCount { get; private set; }

            public void SetProgramSettings(string custCode, ProgramSettings settings) => _settings[custCode] = settings;

            public ProgramSettings? GetCustomizeProgramSettings(string custCode)
            {
                GetCustomizeProgramSettingsCallCount++;
                return _settings.TryGetValue(custCode, out var s) ? s : null;
            }

            public LanguageResource? GetCustomizeLanguage(string custCode, string lang, string ns) => null;
            public FormLayout? GetCustomizeFormLayout(string custCode, string layoutId) => null;
        }

        private sealed class ProgramSettingsDefineAccess : IDefineAccess
        {
            public ProgramSettings Current { get; set; }
            public ProgramSettingsDefineAccess(ProgramSettings initial) { Current = initial; }
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

        private sealed class ThrowingProgramSettingsDefineAccess : IDefineAccess
        {
            public ProgramSettings GetProgramSettings() => throw new FileNotFoundException("ProgramSettings.xml not found");

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
