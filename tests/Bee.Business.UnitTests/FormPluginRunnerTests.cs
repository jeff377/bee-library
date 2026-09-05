using System.ComponentModel;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="FormPluginChain"/> 與 <see cref="FormPluginRunner"/>：宣告時點與類別覆寫必須
    /// 精確對帳、依宣告順序執行、實例按需建構，以及每次操作各自的實例。
    /// </summary>
    public class FormPluginRunnerTests : IClassFixture<BeeTestFixture>
    {
        private readonly IBeeContext _ctx;

        public FormPluginRunnerTests(BeeTestFixture fixture)
        {
            _ctx = TestBeeContext.Create(fixture);
        }

        private static FormPluginChain Chain(params FormPluginBinding[] bindings)
            => FormPluginChain.Create("Order", bindings);

        private FormPluginRunner CreateRunner(params FormPluginBinding[] bindings)
            => Chain(bindings).CreateRunner(_ctx, Guid.NewGuid(), "Order");

        private static FormPluginBinding Bind<T>(PluginStage stage) where T : FormBusinessPlugin
            => new(typeof(T), stage);

        [Fact]
        [DisplayName("chain 依宣告記錄各型別的時點")]
        public void Chain_RecordsDeclaredStage()
        {
            var chain = Chain(Bind<BeforeSaveOnlyPlugin>(PluginStage.BeforeSave));

            Assert.True(chain.HasStage(PluginStage.BeforeSave));
            Assert.False(chain.HasStage(PluginStage.AfterSave));
            Assert.False(chain.HasStage(PluginStage.BeforeDelete));
            Assert.False(chain.HasStage(PluginStage.AfterDelete));
        }

        [Fact]
        [DisplayName("chain 對每個時點回傳該時點會跑的型別（維護工具的可讀性來源）")]
        public void Chain_TypesForStage_ListsOnlyThatStage()
        {
            var chain = Chain(
                Bind<AfterSaveOnlyPlugin>(PluginStage.AfterSave),
                Bind<BeforeSaveOnlyPlugin>(PluginStage.BeforeSave));

            Assert.Equal([typeof(BeforeSaveOnlyPlugin)], chain.TypesForStage(PluginStage.BeforeSave));
            Assert.Equal([typeof(AfterSaveOnlyPlugin)], chain.TypesForStage(PluginStage.AfterSave));
        }

        [Fact]
        [DisplayName("★宣告的時點與類別覆寫的不同時拒絕建鏈，訊息指出類別實際覆寫的是哪一個")]
        public void Create_DeclaredStageDisagreesWithOverride_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Chain(Bind<BeforeSaveOnlyPlugin>(PluginStage.AfterSave)));

            Assert.Contains("Stage=\"AfterSave\"", ex.Message, StringComparison.Ordinal);
            Assert.Contains("overrides BeforeSave", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("★沒宣告 Stage（手寫檔漏打）時拒絕建鏈，訊息把正確答案寫出來")]
        public void Create_NoStageDeclared_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Chain(Bind<BeforeSaveOnlyPlugin>(PluginStage.None)));

            Assert.Contains("with no Stage", ex.Message, StringComparison.Ordinal);
            Assert.Contains("declare Stage=\"BeforeSave\"", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("★一個類別覆寫兩個時點時拒絕建鏈——一個 plugin 只掛一個時點")]
        public void Create_TypeOverridesTwoStages_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Chain(Bind<BothSaveStagesPlugin>(PluginStage.BeforeSave)));

            Assert.Contains("overrides BeforeSave and AfterSave", ex.Message, StringComparison.Ordinal);
            Assert.Contains("one class per stage", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("一個時點都沒覆寫的 plugin 拒絕建鏈——掛了等於沒掛")]
        public void Create_TypeOverridesNothing_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Chain(Bind<NoStagePlugin>(PluginStage.BeforeSave)));

            Assert.Contains("overrides no stage", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("空 chain 不建構任何實例，也不影響管線")]
        public void EmptyChain_RunsNothing()
        {
            RecordingPlugin.Reset();
            var runner = CreateRunner();

            runner.RunBeforeSave(null!);
            runner.RunAfterSave(null!);

            Assert.True(FormPluginChain.Empty.IsEmpty);
            Assert.Equal(0, RecordingPlugin.ConstructedCount);
        }

        [Fact]
        [DisplayName("多個 plugin 依宣告順序執行")]
        public void Run_ExecutesInDeclarationOrder()
        {
            RecordingPlugin.Reset();
            var runner = CreateRunner(
                Bind<FirstPlugin>(PluginStage.BeforeSave),
                Bind<SecondPlugin>(PluginStage.BeforeSave));

            runner.RunBeforeSave(null!);

            Assert.Equal(["First.BeforeSave", "Second.BeforeSave"], RecordingPlugin.Calls);
        }

        [Fact]
        [DisplayName("不是該時點的 plugin 不會被叫到")]
        public void Run_SkipsPluginsBoundToAnotherStage()
        {
            RecordingPlugin.Reset();
            var runner = CreateRunner(
                Bind<BeforeSaveOnlyPlugin>(PluginStage.BeforeSave),
                Bind<AfterSaveOnlyPlugin>(PluginStage.AfterSave));

            runner.RunBeforeSave(null!);

            Assert.Equal(["BeforeSaveOnly.BeforeSave"], RecordingPlugin.Calls);
        }

        [Fact]
        [DisplayName("★實例按需建構：跑 save 時不建構只掛 delete 時點的 plugin")]
        public void Run_ConstructsOnlyThePluginsOfTheStageBeingRun()
        {
            RecordingPlugin.Reset();
            var runner = CreateRunner(
                Bind<BeforeSaveOnlyPlugin>(PluginStage.BeforeSave),
                Bind<BeforeDeleteOnlyPlugin>(PluginStage.BeforeDelete));

            runner.RunBeforeSave(null!);

            // 舊設計為了讓後面的時點找到同一個物件，一建就建整條鏈；一個 plugin 只掛一個時點後，
            // 那個理由不存在了，delete-only 的 plugin 在 save 操作中不該被建出來。
            Assert.Equal(1, RecordingPlugin.ConstructedCount);
            Assert.Equal(["BeforeSaveOnly.BeforeSave"], RecordingPlugin.Calls);
        }

        [Fact]
        [DisplayName("完全沒用到的 chain 一個實例都不建構")]
        public void Run_UnusedStage_ConstructsNothing()
        {
            RecordingPlugin.Reset();
            var runner = CreateRunner(Bind<BeforeSaveOnlyPlugin>(PluginStage.BeforeSave));

            runner.RunBeforeDelete(null!);

            Assert.Equal(0, RecordingPlugin.ConstructedCount);
        }

        [Fact]
        [DisplayName("同一次操作的同一個 plugin 只建構一次")]
        public void Run_SameOperation_ConstructsEachPluginOnce()
        {
            RecordingPlugin.Reset();
            var runner = CreateRunner(Bind<BeforeSaveOnlyPlugin>(PluginStage.BeforeSave));

            runner.RunBeforeSave(null!);
            runner.RunBeforeSave(null!);

            Assert.Equal(1, RecordingPlugin.ConstructedCount);
        }

        [Fact]
        [DisplayName("不同次操作是不同實例，狀態不外洩到下一次呼叫")]
        public void Run_DifferentOperations_DoNotShareInstances()
        {
            RecordingPlugin.Reset();
            var chain = Chain(Bind<CountingPlugin>(PluginStage.BeforeSave));

            chain.CreateRunner(_ctx, Guid.NewGuid(), "Order").RunBeforeSave(null!);
            chain.CreateRunner(_ctx, Guid.NewGuid(), "Order").RunBeforeSave(null!);

            Assert.Equal(2, RecordingPlugin.ConstructedCount);
            // 兩次都是 seen=1：第二個實例沒有繼承第一個的欄位值。
            Assert.Equal(["Counting.BeforeSave(seen=1)", "Counting.BeforeSave(seen=1)"], RecordingPlugin.Calls);
        }

        [Fact]
        [DisplayName("plugin 可在三個定位參數之外宣告自己的注入相依（ActivatorUtilities）")]
        public void Run_ConstructsWithInjectedDependencies()
        {
            RecordingPlugin.Reset();
            var runner = CreateRunner(Bind<InjectedPlugin>(PluginStage.BeforeSave));

            runner.RunBeforeSave(null!);

            // 前三個仍是位置參數，第四個由容器解析——按需建構沒有改變這個行為。
            Assert.Equal(["Injected.BeforeSave(progId=Order, injected=yes)"], RecordingPlugin.Calls);
        }

        // ---- Test plugins ----

        /// <summary>共用的呼叫記錄，測試間以 <see cref="Reset"/> 隔離。</summary>
        public abstract class RecordingPlugin : FormBusinessPlugin
        {
            protected RecordingPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId)
            {
                ConstructedCount++;
            }

            public static List<string> Calls { get; } = [];

            public static int ConstructedCount { get; private set; }

            public static void Reset()
            {
                Calls.Clear();
                ConstructedCount = 0;
            }

            protected static void Record(string call) => Calls.Add(call);
        }

        public sealed class BeforeSaveOnlyPlugin : RecordingPlugin
        {
            public BeforeSaveOnlyPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeSave(SaveContext context) => Record("BeforeSaveOnly.BeforeSave");
        }

        public sealed class AfterSaveOnlyPlugin : RecordingPlugin
        {
            public AfterSaveOnlyPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void AfterSave(SaveContext context) => Record("AfterSaveOnly.AfterSave");
        }

        public sealed class BeforeDeleteOnlyPlugin : RecordingPlugin
        {
            public BeforeDeleteOnlyPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeDelete(DeleteContext context) => Record("BeforeDeleteOnly.BeforeDelete");
        }

        /// <summary>覆寫兩個時點，用於驗證「一個 plugin 一個時點」的拒絕。</summary>
        public sealed class BothSaveStagesPlugin : RecordingPlugin
        {
            public BothSaveStagesPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeSave(SaveContext context) => Record("Both.BeforeSave");

            public override void AfterSave(SaveContext context) => Record("Both.AfterSave");
        }

        /// <summary>什麼都沒覆寫，掛了等於沒掛。</summary>
        public sealed class NoStagePlugin : RecordingPlugin
        {
            public NoStagePlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }
        }

        /// <summary>以 instance field 計數，用於驗證實例不跨操作共用。</summary>
        public sealed class CountingPlugin : RecordingPlugin
        {
            private int _seen;

            public CountingPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeSave(SaveContext context)
            {
                _seen++;
                Record($"Counting.BeforeSave(seen={_seen})");
            }
        }

        /// <summary>三個定位參數之外還要一個由容器解析的相依。</summary>
        public sealed class InjectedPlugin : RecordingPlugin
        {
            private readonly IDefineAccess _defineAccess;

            public InjectedPlugin(IBeeContext ctx, Guid accessToken, string progId, IDefineAccess defineAccess)
                : base(ctx, accessToken, progId)
            {
                _defineAccess = defineAccess;
            }

            public override void BeforeSave(SaveContext context)
                => Record($"Injected.BeforeSave(progId={ProgId}, injected={(_defineAccess is null ? "no" : "yes")})");
        }

        public sealed class FirstPlugin : RecordingPlugin
        {
            public FirstPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeSave(SaveContext context) => Record("First.BeforeSave");
        }

        public sealed class SecondPlugin : RecordingPlugin
        {
            public SecondPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeSave(SaveContext context) => Record("Second.BeforeSave");
        }
    }
}
