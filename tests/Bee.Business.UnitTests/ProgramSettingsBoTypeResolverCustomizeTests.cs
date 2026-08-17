using System.ComponentModel;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Microsoft.Extensions.Logging;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="ProgramSettingsBoTypeResolver"/> 租戶客製化疊加測試：cust 有 progId→客製 BO；
    /// cust 無→base BO；type cache 以 (customizeId, progId) 隔離；customizeId 空 / 無 reader→短路純 base。
    /// </summary>
    public class ProgramSettingsBoTypeResolverCustomizeTests
    {
        public class TenantFormBo : FormBusinessObject
        {
            public TenantFormBo(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
                : base(ctx, accessToken, progId, isLocalCall) { }
        }

        private static string TenantFormBoFqn =>
            $"{typeof(TenantFormBo).FullName}, {typeof(TenantFormBo).Assembly.GetName().Name}";

        private static string BaseFormBoFqn =>
            $"{typeof(ProgramSettingsBoTypeResolverTests.TestableCustomFormBo).FullName}, " +
            $"{typeof(ProgramSettingsBoTypeResolverTests.TestableCustomFormBo).Assembly.GetName().Name}";

        private static ProgramSettings BuildSettings(params (string progId, string? businessObject)[] items)
        {
            var settings = new ProgramSettings();
            foreach (var (progId, businessObject) in items)
            {
                var item = settings.Items!.Add(progId, progId);
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
            var resolver = new ProgramSettingsBoTypeResolver(defineAccess, reader);

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
            var resolver = new ProgramSettingsBoTypeResolver(defineAccess, reader);

            var result = resolver.Resolve("acme", "P001");

            Assert.Equal(typeof(ProgramSettingsBoTypeResolverTests.TestableCustomFormBo), result);
        }

        [Fact]
        [DisplayName("type cache 以 (customizeId, progId) 隔離：不同租戶同一 progId 解析互不干擾")]
        public void Resolve_DifferentCustomizeIds_IsolatedCache()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("P001", BaseFormBoFqn)));
            var reader = new SpyCustomizeReader();
            reader.SetProgramSettings("acme", BuildSettings(("P001", TenantFormBoFqn)));
            // globex 沒有客製
            var resolver = new ProgramSettingsBoTypeResolver(defineAccess, reader);

            var acme = resolver.Resolve("acme", "P001");
            var globex = resolver.Resolve("globex", "P001");
            var baseOnly = resolver.Resolve("P001");

            Assert.Equal(typeof(TenantFormBo), acme);
            Assert.Equal(typeof(ProgramSettingsBoTypeResolverTests.TestableCustomFormBo), globex);
            Assert.Equal(typeof(ProgramSettingsBoTypeResolverTests.TestableCustomFormBo), baseOnly);
        }

        [Fact]
        [DisplayName("customizeId 空時短路純 base，reader 零呼叫")]
        public void Resolve_EmptyCustomizeId_ShortCircuits_ReaderNotCalled()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("P001", BaseFormBoFqn)));
            var reader = new SpyCustomizeReader();
            reader.SetProgramSettings("acme", BuildSettings(("P001", TenantFormBoFqn)));
            var resolver = new ProgramSettingsBoTypeResolver(defineAccess, reader);

            var result = resolver.Resolve("P001");

            Assert.Equal(typeof(ProgramSettingsBoTypeResolverTests.TestableCustomFormBo), result);
            Assert.Equal(0, reader.GetCustomizeProgramSettingsCallCount);
        }

        [Fact]
        [DisplayName("無 reader 注入時即使帶 customizeId 也走純 base（向後相容）")]
        public void Resolve_NoReader_BehavesAsBase()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("P001", BaseFormBoFqn)));
            var resolver = new ProgramSettingsBoTypeResolver(defineAccess); // 無 reader

            var result = resolver.Resolve("acme", "P001");

            Assert.Equal(typeof(ProgramSettingsBoTypeResolverTests.TestableCustomFormBo), result);
        }

        [Fact]
        [DisplayName("base ProgramSettings 缺檔但 cust 有該 progId 時仍解析為客製 BO")]
        public void Resolve_BaseMissingButCustHasProgId_ReturnsCustomizeBo()
        {
            var defineAccess = new ThrowingProgramSettingsDefineAccess();
            var reader = new SpyCustomizeReader();
            reader.SetProgramSettings("acme", BuildSettings(("P001", TenantFormBoFqn)));
            var resolver = new ProgramSettingsBoTypeResolver(defineAccess, reader);

            var result = resolver.Resolve("acme", "P001");

            Assert.Equal(typeof(TenantFormBo), result);
        }

        // ---- 解析失敗的可觀測性 ----

        [Fact]
        [DisplayName("客製 BO 型別載不到時應拋出，訊息標示客製來源")]
        public void Resolve_CustomizeTypeUnloadable_ThrowsWithCustomizeOrigin()
        {
            var reader = new SpyCustomizeReader();
            reader.SetProgramSettings("acme", BuildSettings(("Order", "Acme.Typo.OrderBo, Acme.Typo")));
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("Order", BaseFormBoFqn)));
            var resolver = new ProgramSettingsBoTypeResolver(defineAccess, reader);

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("acme", "Order"));

            Assert.Contains("Order", ex.Message, StringComparison.Ordinal);
            Assert.Contains("Acme.Typo.OrderBo, Acme.Typo", ex.Message, StringComparison.Ordinal);
            Assert.Contains("acme", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("型別不繼承 BusinessObject 時應拋出，訊息標示套裝來源")]
        public void Resolve_TypeNotBusinessObject_ThrowsWithBaseOrigin()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("Order", NotABusinessObjectFqn)));
            var resolver = new ProgramSettingsBoTypeResolver(defineAccess, null);

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("Order"));

            Assert.Contains("base registry", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("失敗不進 type cache：重複呼叫每次都應拋出，不會第二次起靜默通過")]
        public void Resolve_RepeatedFailure_ThrowsEveryTime()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("Order", "Nope.OrderBo, Nope")));
            var resolver = new ProgramSettingsBoTypeResolver(defineAccess, null);

            Assert.Throws<InvalidOperationException>(() => resolver.Resolve("Order"));
            Assert.Throws<InvalidOperationException>(() => resolver.Resolve("Order"));
            Assert.Throws<InvalidOperationException>(() => resolver.Resolve("Order"));
        }

        [Fact]
        [DisplayName("帶 logger 的建構子多載保留可用（logger 已不再使用，不應收到任何訊息）")]
        public void Ctor_LoggerOverload_StillResolvesAndLogsNothing()
        {
            var defineAccess = new ProgramSettingsDefineAccess(BuildSettings(("Order", BaseFormBoFqn)));
            var logger = new RecordingLogger();
            var resolver = new ProgramSettingsBoTypeResolver(defineAccess, null, logger);

            var result = resolver.Resolve("Order");

            Assert.Equal(typeof(ProgramSettingsBoTypeResolverTests.TestableCustomFormBo), result);
            Assert.Empty(logger.Entries);
        }

        // ---- Test doubles ----

        /// <summary>不繼承 <see cref="BusinessObject"/> 的型別，用於驗證「型別不相容」的失敗路徑。</summary>
        public sealed class NotABusinessObject { }

        private static string NotABusinessObjectFqn =>
            $"{typeof(NotABusinessObject).FullName}, {typeof(NotABusinessObject).Assembly.GetName().Name}";

        private sealed class RecordingLogger : ILogger<ProgramSettingsBoTypeResolver>
        {
            public List<(LogLevel Level, string Message)> Entries { get; } = [];

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
                => Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class SpyCustomizeReader : ICustomizeDefineReader
        {
            private readonly Dictionary<string, ProgramSettings> _settings = new(StringComparer.Ordinal);

            public int GetCustomizeProgramSettingsCallCount { get; private set; }

            public void SetProgramSettings(string customizeId, ProgramSettings settings) => _settings[customizeId] = settings;

            public ProgramSettings? GetCustomizeProgramSettings(string customizeId)
            {
                GetCustomizeProgramSettingsCallCount++;
                return _settings.TryGetValue(customizeId, out var s) ? s : null;
            }

            public LanguageResource? GetCustomizeLanguage(string customizeId, string lang, string ns) => null;
            public FormLayout? GetCustomizeFormLayout(string customizeId, string layoutId) => null;
            public MenuSettings? GetCustomizeMenuSettings(string customizeId) => null;
            public PluginSettings? GetCustomizePluginSettings(string customizeId) => null;
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
