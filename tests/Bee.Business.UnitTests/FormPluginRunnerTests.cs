using System.ComponentModel;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="FormPluginChain"/> 與 <see cref="FormPluginRunner"/>：反射判定各時點、
    /// 依宣告順序執行、沒 override 的時點不被叫到，以及**每次操作一個實例**的生命週期保證。
    /// </summary>
    public class FormPluginRunnerTests : IClassFixture<BeeTestFixture>
    {
        private readonly IBeeContext _ctx;

        public FormPluginRunnerTests(BeeTestFixture fixture)
        {
            _ctx = TestBeeContext.Create(fixture);
        }

        private FormPluginRunner CreateRunner(params Type[] types)
            => FormPluginChain.Create(types).CreateRunner(_ctx, Guid.NewGuid(), "Order");

        [Fact]
        [DisplayName("chain 以反射判定各型別實作了哪些時點")]
        public void Chain_DetectsOverriddenStagesOnly()
        {
            var chain = FormPluginChain.Create([typeof(BeforeSaveOnlyPlugin)]);

            Assert.True(chain.HasStage(FormPluginStage.BeforeSave));
            Assert.False(chain.HasStage(FormPluginStage.AfterSave));
            Assert.False(chain.HasStage(FormPluginStage.BeforeDelete));
            Assert.False(chain.HasStage(FormPluginStage.AfterDelete));
        }

        [Fact]
        [DisplayName("chain 對每個時點回傳該時點實際會跑的型別（維護工具的可讀性來源）")]
        public void Chain_TypesForStage_ListsOnlyImplementors()
        {
            var chain = FormPluginChain.Create([typeof(AfterSaveOnlyPlugin), typeof(BothSaveStagesPlugin)]);

            Assert.Equal([typeof(BothSaveStagesPlugin)], chain.TypesForStage(FormPluginStage.BeforeSave));
            Assert.Equal([typeof(AfterSaveOnlyPlugin), typeof(BothSaveStagesPlugin)],
                chain.TypesForStage(FormPluginStage.AfterSave));
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
            var runner = CreateRunner(typeof(FirstPlugin), typeof(SecondPlugin));

            runner.RunBeforeSave(null!);

            Assert.Equal(["First.BeforeSave", "Second.BeforeSave"], RecordingPlugin.Calls);
        }

        [Fact]
        [DisplayName("沒有 override 該時點的 plugin 不會被叫到")]
        public void Run_SkipsPluginsThatDoNotImplementTheStage()
        {
            RecordingPlugin.Reset();
            var runner = CreateRunner(typeof(BeforeSaveOnlyPlugin), typeof(AfterSaveOnlyPlugin));

            runner.RunBeforeSave(null!);

            Assert.Equal(["BeforeSaveOnly.BeforeSave"], RecordingPlugin.Calls);
        }

        [Fact]
        [DisplayName("★同一次操作的各時點共用同一個 plugin 實例，狀態可跨時點傳遞")]
        public void Run_SameOperation_SharesOneInstanceAcrossStages()
        {
            RecordingPlugin.Reset();
            var runner = CreateRunner(typeof(BothSaveStagesPlugin));

            runner.RunBeforeSave(null!);
            runner.RunAfterSave(null!);

            // 只建構一次；BeforeSave 寫進 instance field 的值在 AfterSave 讀得到。
            Assert.Equal(1, RecordingPlugin.ConstructedCount);
            Assert.Equal(["Both.BeforeSave", "Both.AfterSave(saw=42)"], RecordingPlugin.Calls);
        }

        [Fact]
        [DisplayName("不同次操作是不同實例，狀態不外洩到下一次呼叫")]
        public void Run_DifferentOperations_DoNotShareInstances()
        {
            RecordingPlugin.Reset();
            var chain = FormPluginChain.Create([typeof(BothSaveStagesPlugin)]);

            chain.CreateRunner(_ctx, Guid.NewGuid(), "Order").RunBeforeSave(null!);
            chain.CreateRunner(_ctx, Guid.NewGuid(), "Order").RunAfterSave(null!);

            Assert.Equal(2, RecordingPlugin.ConstructedCount);
            // 第二個 runner 的實例沒跑過 BeforeSave，所以欄位still是預設值。
            Assert.Equal(["Both.BeforeSave", "Both.AfterSave(saw=0)"], RecordingPlugin.Calls);
        }

        [Fact]
        [DisplayName("實例延遲到第一次用到才建構——只跑 delete 時點時不建構 save-only 的鏈")]
        public void Run_ConstructsLazilyOnFirstUse()
        {
            RecordingPlugin.Reset();
            var runner = CreateRunner(typeof(BeforeSaveOnlyPlugin));

            runner.RunBeforeDelete(null!);

            Assert.Equal(0, RecordingPlugin.ConstructedCount);
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

        /// <summary>跨時點傳遞狀態的那個案例：BeforeSave 算出值，AfterSave 讀它。</summary>
        public sealed class BothSaveStagesPlugin : RecordingPlugin
        {
            private int _computed;

            public BothSaveStagesPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeSave(SaveContext context)
            {
                _computed = 42;
                Record("Both.BeforeSave");
            }

            public override void AfterSave(SaveContext context) => Record($"Both.AfterSave(saw={_computed})");
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
