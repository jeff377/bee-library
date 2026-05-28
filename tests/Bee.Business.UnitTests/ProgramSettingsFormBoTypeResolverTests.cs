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
    /// <see cref="ProgramSettingsFormBoTypeResolver"/> 的單元測試。
    /// </summary>
    public class ProgramSettingsFormBoTypeResolverTests
    {
        // Test subclass used to verify the "valid FormBusinessObject derivative" branch.
        // Reachable via its assembly-qualified type name from inside the same test assembly.
        public class TestableCustomFormBo : FormBusinessObject
        {
            public TestableCustomFormBo(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
                : base(ctx, accessToken, progId, isLocalCall) { }
        }

        private static string TestableCustomFormBoFqn =>
            $"{typeof(TestableCustomFormBo).FullName}, {typeof(TestableCustomFormBo).Assembly.GetName().Name}";

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
        [DisplayName("ProgramSettings.xml 不存在(FileNotFoundException)時應 fallback 回 FormBusinessObject")]
        public void Resolve_ProgramSettingsFileMissing_FallsBackToFormBusinessObject()
        {
            var defineAccess = new ThrowingDefineAccess();
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess);

            var result = resolver.Resolve("P001");

            Assert.Equal(typeof(FormBusinessObject), result);
        }

        [Fact]
        [DisplayName("ProgId 不在 ProgramSettings 時應回傳 FormBusinessObject")]
        public void Resolve_ProgIdNotRegistered_ReturnsFormBusinessObject()
        {
            var defineAccess = new ProgramSettingsDefineAccess(new ProgramSettings());
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess);

            var result = resolver.Resolve("UNKNOWN");

            Assert.Equal(typeof(FormBusinessObject), result);
        }

        [Fact]
        [DisplayName("ProgId 存在但 BusinessObject 為空字串時應回傳 FormBusinessObject")]
        public void Resolve_BusinessObjectEmpty_ReturnsFormBusinessObject()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("P001", null)));
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess);

            var result = resolver.Resolve("P001");

            Assert.Equal(typeof(FormBusinessObject), result);
        }

        [Fact]
        [DisplayName("BusinessObject 指向不存在的型別時應 fallback 回 FormBusinessObject")]
        public void Resolve_BusinessObjectUnresolvable_FallsBackToFormBusinessObject()
        {
            var settings = BuildSettings(("P001", "NonExistent.Bo, NonExistent.Assembly"));
            var defineAccess = new ProgramSettingsDefineAccess(settings);
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess);

            var result = resolver.Resolve("P001");

            Assert.Equal(typeof(FormBusinessObject), result);
        }

        [Fact]
        [DisplayName("BusinessObject 指向非 FormBusinessObject 子類時應 fallback 回 FormBusinessObject")]
        public void Resolve_BusinessObjectNotAssignable_FallsBackToFormBusinessObject()
        {
            // System.Object is a real type but not assignable to FormBusinessObject.
            var settings = BuildSettings(("P001", "System.Object, System.Private.CoreLib"));
            var defineAccess = new ProgramSettingsDefineAccess(settings);
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess);

            var result = resolver.Resolve("P001");

            Assert.Equal(typeof(FormBusinessObject), result);
        }

        [Fact]
        [DisplayName("BusinessObject 指向合法 FormBusinessObject 子類時應回傳該型別")]
        public void Resolve_BusinessObjectValid_ReturnsCustomType()
        {
            var settings = BuildSettings(("P001", TestableCustomFormBoFqn));
            var defineAccess = new ProgramSettingsDefineAccess(settings);
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess);

            var result = resolver.Resolve("P001");

            Assert.Equal(typeof(TestableCustomFormBo), result);
        }

        [Fact]
        [DisplayName("Resolve 同一 ProgId 多次應走 cache,不重複查 ProgramSettings 內容")]
        public void Resolve_SameProgIdTwice_UsesCache()
        {
            var settings = BuildSettings(("P001", TestableCustomFormBoFqn));
            var defineAccess = new ProgramSettingsDefineAccess(settings);
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess);

            var first = resolver.Resolve("P001");

            // Mutate the BusinessObject after first resolve; if cache works the
            // second call must still return the original type.
            settings.Categories!["C01"].Items!["P001"].BusinessObject = "Garbage, Garbage";

            var second = resolver.Resolve("P001");

            Assert.Equal(typeof(TestableCustomFormBo), first);
            Assert.Equal(typeof(TestableCustomFormBo), second);
        }

        [Fact]
        [DisplayName("ProgramSettings 實例切換時應 reset cache 並重新解析")]
        public void Resolve_SettingsInstanceReplaced_ResetsCache()
        {
            var first = BuildSettings(("P001", TestableCustomFormBoFqn));
            var defineAccess = new ProgramSettingsDefineAccess(first);
            var resolver = new ProgramSettingsFormBoTypeResolver(defineAccess);

            var initial = resolver.Resolve("P001");
            Assert.Equal(typeof(TestableCustomFormBo), initial);

            // Simulate a file-watcher reload: hand back a different instance whose
            // P001 now declares no BusinessObject.
            defineAccess.Current = BuildSettings(("P001", null));

            var reloaded = resolver.Resolve("P001");

            Assert.Equal(typeof(FormBusinessObject), reloaded);
        }

        /// <summary>
        /// 測試用 <see cref="IDefineAccess"/>,模擬 ProgramSettings.xml 不存在的情境,
        /// 對 <see cref="GetProgramSettings"/> 拋出 <see cref="FileNotFoundException"/>。
        /// </summary>
        private sealed class ThrowingDefineAccess : IDefineAccess
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

        /// <summary>
        /// 測試用 <see cref="IDefineAccess"/>,允許外部替換 <see cref="ProgramSettings"/> 實例
        /// 以驗證 reference-equality 觸發的 cache reset 行為。
        /// </summary>
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
    }
}
