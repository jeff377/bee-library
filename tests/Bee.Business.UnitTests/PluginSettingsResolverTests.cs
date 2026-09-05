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
    /// <see cref="PluginSettingsResolver"/>：套裝／客製兩層相加、失敗一律拋、
    /// 定義重載後 chain cache 重建。
    /// </summary>
    public class PluginSettingsResolverTests
    {
        private static string SamplePluginFqn =>
            $"{typeof(SamplePlugin).FullName}, {typeof(SamplePlugin).Assembly.GetName().Name}";

        private static string OtherPluginFqn =>
            $"{typeof(OtherPlugin).FullName}, {typeof(OtherPlugin).Assembly.GetName().Name}";

        private static string NotAPluginFqn =>
            $"{typeof(NotAPlugin).FullName}, {typeof(NotAPlugin).Assembly.GetName().Name}";

        private static PluginSettings Build(string progId, params (string Type, PluginStage Stage)[] plugins)
        {
            var settings = new PluginSettings();
            var program = settings.Items!.Add(progId);
            foreach (var (type, stage) in plugins)
                program.Plugins!.Add(type, stage);
            return settings;
        }

        /// <summary><see cref="SamplePlugin"/> 覆寫 BeforeSave，宣告與覆寫相符。</summary>
        private static (string, PluginStage) Sample => (SamplePluginFqn, PluginStage.BeforeSave);

        /// <summary><see cref="OtherPlugin"/> 覆寫 AfterSave，宣告與覆寫相符。</summary>
        private static (string, PluginStage) Other => (OtherPluginFqn, PluginStage.AfterSave);

        [Fact]
        [DisplayName("套裝層的鏈解析為對應型別")]
        public void Resolve_BaseOnly_ReturnsBaseChain()
        {
            var access = new StubDefineAccess(Build("Order", Sample));
            var resolver = new PluginSettingsResolver(access);

            Assert.Equal([typeof(SamplePlugin)], resolver.Resolve("", "Order").Types);
        }

        [Fact]
        [DisplayName("兩層相加：套裝在前、客製在後")]
        public void Resolve_BothLayers_ConcatenatesBaseThenCustomize()
        {
            var access = new StubDefineAccess(Build("Order", Sample));
            var reader = new StubCustomizeReader { Settings = Build("Order", Other) };
            var resolver = new PluginSettingsResolver(access, reader);

            Assert.Equal([typeof(SamplePlugin), typeof(OtherPlugin)],
                resolver.Resolve("acme", "Order").Types);
        }

        [Fact]
        [DisplayName("未綁定任何 plugin 的 progId 回空 chain")]
        public void Resolve_NoBinding_ReturnsEmptyChain()
        {
            var resolver = new PluginSettingsResolver(new StubDefineAccess(new PluginSettings()));

            Assert.True(resolver.Resolve("", "Order").IsEmpty);
        }

        [Fact]
        [DisplayName("兩層都沒有定義檔時回空 chain，不是 null——這是絕大多數部署的狀態")]
        public void Resolve_NeitherLayerHasSettings_ReturnsEmptyChainNotNull()
        {
            // base 缺檔（storage 丟 FileNotFoundException）+ 客製缺檔（reader 回 null）。
            var access = new StubDefineAccess(null);
            var reader = new StubCustomizeReader { Settings = null };
            var resolver = new PluginSettingsResolver(access, reader);

            var chain = resolver.Resolve("acme", "Order");

            Assert.NotNull(chain);
            Assert.True(chain.IsEmpty);
            Assert.Empty(chain.Types);
            Assert.False(chain.HasStage(PluginStage.BeforeSave));
        }

        [Fact]
        [DisplayName("兩層都沒有定義檔時 runner 仍可建立，四個時點皆為 no-op")]
        public void Resolve_NeitherLayerHasSettings_RunnerIsUsableNoOp()
        {
            var resolver = new PluginSettingsResolver(new StubDefineAccess(null), new StubCustomizeReader());

            var runner = resolver.Resolve("acme", "Order").CreateRunner(new StubBeeContext(), Guid.NewGuid(), "Order");

            // 不丟例外、不建構任何東西——FormBusinessObject 因此可以無條件呼叫。
            var exception = Record.Exception(() =>
            {
                runner.RunBeforeSave(null!);
                runner.RunAfterSave(null!);
                runner.RunBeforeDelete(null!);
                runner.RunAfterDelete(null!);
            });
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("套裝定義缺檔時不算錯誤，客製仍解析得到")]
        public void Resolve_BaseMissing_StillResolvesCustomize()
        {
            var access = new StubDefineAccess(null);
            var reader = new StubCustomizeReader { Settings = Build("Order", Sample) };
            var resolver = new PluginSettingsResolver(access, reader);

            Assert.Equal([typeof(SamplePlugin)], resolver.Resolve("acme", "Order").Types);
        }

        [Fact]
        [DisplayName("型別載不到時拋例外——plugin 是刻意加上的，靜默略過等於客製沒生效")]
        public void Resolve_UnloadableType_Throws()
        {
            var access = new StubDefineAccess(Build("Order", ("Nope.Missing, Nope", PluginStage.BeforeSave)));
            var resolver = new PluginSettingsResolver(access);

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("", "Order"));
            Assert.Contains("Nope.Missing, Nope", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("型別不繼承 FormBusinessPlugin 時拋例外")]
        public void Resolve_TypeNotAPlugin_Throws()
        {
            var access = new StubDefineAccess(Build("Order", (NotAPluginFqn, PluginStage.BeforeSave)));
            var resolver = new PluginSettingsResolver(access);

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("", "Order"));
            Assert.Contains(nameof(FormBusinessPlugin), ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("★宣告的時點與類別覆寫的不符時拋例外——手寫檔沒有維護 API 把關，這道是唯一的閘門")]
        public void Resolve_DeclaredStageDisagreesWithOverride_Throws()
        {
            var access = new StubDefineAccess(Build("Order", (SamplePluginFqn, PluginStage.AfterDelete)));
            var resolver = new PluginSettingsResolver(access);

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("", "Order"));
            Assert.Contains("overrides BeforeSave", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("★手寫檔漏了 Stage 屬性時拋例外，訊息說得出「你沒宣告」")]
        public void Resolve_NoStageDeclared_Throws()
        {
            var access = new StubDefineAccess(Build("Order", (SamplePluginFqn, PluginStage.None)));
            var resolver = new PluginSettingsResolver(access);

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("", "Order"));
            Assert.Contains("with no Stage", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("cache 以 (customizeId, progId) 隔離：不同租戶互不干擾")]
        public void Resolve_DifferentCustomizeIds_Isolated()
        {
            var access = new StubDefineAccess(Build("Order", Sample));
            var reader = new StubCustomizeReader { Settings = Build("Order", Other) };
            var resolver = new PluginSettingsResolver(access, reader);

            var acme = resolver.Resolve("acme", "Order");
            reader.Settings = null;   // globex 沒有客製檔
            var globex = resolver.Resolve("globex", "Order");

            Assert.Equal([typeof(SamplePlugin), typeof(OtherPlugin)], acme.Types);
            Assert.Equal([typeof(SamplePlugin)], globex.Types);
        }

        [Fact]
        [DisplayName("定義實例更換（file-watcher 重載）後 chain cache 重建")]
        public void Resolve_SettingsInstanceChanged_RebuildsChain()
        {
            var access = new StubDefineAccess(Build("Order", Sample));
            var resolver = new PluginSettingsResolver(access);
            Assert.Equal([typeof(SamplePlugin)], resolver.Resolve("", "Order").Types);

            access.Current = Build("Order", Sample, Other);

            Assert.Equal([typeof(SamplePlugin), typeof(OtherPlugin)], resolver.Resolve("", "Order").Types);
        }

        // ---- Test doubles ----

        public sealed class SamplePlugin : FormBusinessPlugin
        {
            public SamplePlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeSave(SaveContext context) { }
        }

        public sealed class OtherPlugin : FormBusinessPlugin
        {
            public OtherPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void AfterSave(SaveContext context) { }
        }

        /// <summary>不繼承 <see cref="FormBusinessPlugin"/>，用於驗證型別檢查。</summary>
        public sealed class NotAPlugin { }

        /// <summary>空 chain 的 runner 不會碰到 context，所以每個成員都不需要實作。</summary>
        private sealed class StubBeeContext : IBeeContext
        {
            public IDefineAccess DefineAccess => throw new NotImplementedException();
            public Bee.Definition.Identity.ISessionInfoService SessionInfoService => throw new NotImplementedException();
            public ILanguageService LanguageService => throw new NotImplementedException();
            public IBusinessObjectFactory BoFactory => throw new NotImplementedException();
            public IServiceProvider Services => throw new NotImplementedException();
        }

        private sealed class StubCustomizeReader : ICustomizeDefineReader
        {
            public PluginSettings? Settings { get; set; }

            public PluginSettings? GetCustomizePluginSettings(string customizeId) => Settings;

            public LanguageResource? GetCustomizeLanguage(string customizeId, string lang, string ns) => null;
            public ProgramSettings? GetCustomizeProgramSettings(string customizeId) => null;
            public MenuSettings? GetCustomizeMenuSettings(string customizeId) => null;
            public FormLayout? GetCustomizeFormLayout(string customizeId, string layoutId) => null;
        }

        private sealed class StubDefineAccess : IDefineAccess
        {
            public StubDefineAccess(PluginSettings? initial) { Current = initial; }

            public PluginSettings? Current { get; set; }

            public PluginSettings GetPluginSettings()
                => Current ?? throw new FileNotFoundException("PluginSettings.xml not found");

            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public SystemSettings GetSystemSettings() => throw new NotImplementedException();
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
            public LanguageResource GetLanguage(string lang, string ns) => throw new NotImplementedException();
            public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
        }
    }
}
