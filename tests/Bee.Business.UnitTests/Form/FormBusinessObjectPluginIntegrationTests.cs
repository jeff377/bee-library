using System.ComponentModel;
using Bee.Base.Exceptions;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// plugin 走完真實 <see cref="FormBusinessObject.Save"/> / <see cref="FormBusinessObject.Delete"/>
    /// 的端到端行為：掛載點確實在管線的那個位置、改的資料真的落地、例外中止整個操作、
    /// 刪除時點拿得到 <c>Snapshot</c>。
    /// </summary>
    /// <remarks>
    /// 單元層（<c>FormPluginRunnerTests</c>）已釘住 runner 自身的順序與生命週期；這裡驗的是
    /// <see cref="FormBusinessObject"/> 把 runner 接在對的地方——那是單元測試看不到的部分。
    /// </remarks>
    public class FormBusinessObjectPluginIntegrationTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public FormBusinessObjectPluginIntegrationTests(SharedDbFixture fx) { _fx = fx; }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：BeforeSave plugin 改的欄位真的寫進資料庫")]
        public void Save_BeforeSavePlugin_MutationIsPersisted()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                var dataSet = ctx.Repository.GetNewData();
                var master = dataSet.Tables[CrudTestContext.ProgId]!;
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"S{runId}";
                master.Rows[0][SysFields.Name] = "原始名稱";

                ctx.CreateBo(Resolver<RenamingPlugin>(PluginStage.BeforeSave)).Save(new SaveArgs { DataSet = dataSet });

                // BeforeSave 在持久化之前，所以改動要看得見。
                var reloaded = ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId });
                Assert.Equal("BeforeSave 改過",
                    reloaded.DataSet!.Tables[CrudTestContext.ProgId]!.Rows[0][SysFields.Name]);
            }
            finally
            {
                DeleteRow(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：BeforeSave plugin 拋例外時整筆存檔中止，資料未寫入")]
        public void Save_BeforeSavePluginThrows_AbortsWithoutWriting()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            var dataSet = ctx.Repository.GetNewData();
            var master = dataSet.Tables[CrudTestContext.ProgId]!;
            master.Rows[0][SysFields.RowId] = rowId;
            master.Rows[0]["sys_id"] = $"S{runId}";
            master.Rows[0][SysFields.Name] = "不該被存進去";

            var ex = Assert.Throws<UserMessageException>(() =>
                ctx.CreateBo(Resolver<RejectingPlugin>(PluginStage.BeforeSave)).Save(new SaveArgs { DataSet = dataSet }));
            Assert.Equal("擋下這筆。", ex.Message);

            // BeforeSave 在持久化之前中止，所以什麼都不該落地。
            Assert.Null(ctx.CreateBo().GetData(new GetDataArgs { RowId = rowId }).DataSet);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：一次 Save 內兩個時點依管線順序執行，各自是獨立的實例")]
        public void Save_TwoStages_RunInPipelineOrderAsSeparateInstances()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];
            TracingProbe.Reset();

            try
            {
                var dataSet = ctx.Repository.GetNewData();
                var master = dataSet.Tables[CrudTestContext.ProgId]!;
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"S{runId}";
                master.Rows[0][SysFields.Name] = "順序驗證";

                ctx.CreateBo(Resolver(
                        new FormPluginBinding(typeof(BeforeSaveTracingPlugin), PluginStage.BeforeSave),
                        new FormPluginBinding(typeof(AfterSaveTracingPlugin), PluginStage.AfterSave)))
                    .Save(new SaveArgs { DataSet = dataSet });

                // 兩個時點都跑了，且順序由管線決定。一個 plugin 一個時點，所以這是兩個類別、
                // 兩個實例——跨時點沒有共用的 instance field 可傳遞狀態。
                Assert.Equal(["BeforeSave", "AfterSave"], TracingProbe.Calls);
                Assert.Equal(2, TracingProbe.ConstructedCount);
            }
            finally
            {
                DeleteRow(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：AfterSave plugin 看得到 RefreshedDataSet")]
        public void Save_AfterSavePlugin_SeesRefreshedDataSet()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];
            TracingProbe.Reset();

            try
            {
                var dataSet = ctx.Repository.GetNewData();
                var master = dataSet.Tables[CrudTestContext.ProgId]!;
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"S{runId}";
                master.Rows[0][SysFields.Name] = "重讀驗證";

                ctx.CreateBo(Resolver<AfterSaveTracingPlugin>(PluginStage.AfterSave)).Save(new SaveArgs { DataSet = dataSet });

                Assert.True(TracingProbe.AfterSaveHadRefreshedDataSet);
            }
            finally
            {
                DeleteRow(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：★稽核關閉時 AfterDelete 仍拿得到 Snapshot")]
        public void Delete_AfterDeletePlugin_GetsSnapshotWithAuditDisabled()
        {
            // 這是 G2 修正 Snapshot 載入條件的回歸測試，必須在稽核關閉下跑：舊條件是
            // `auditChange || HasBeforeDeleteRules(schema)`，兩者皆不成立時 Snapshot 為 null，
            // 而同步類的 AfterDelete plugin 正需要知道刪掉的是什麼。
            var auditOptions = _fx.Provider.GetService(typeof(Definition.Settings.AuditLogOptions))
                as Definition.Settings.AuditLogOptions;
            Assert.True(auditOptions is not { Enabled: true, ChangeEnabled: true },
                "本測試的前提是變更稽核關閉；若測試環境改為預設開啟，這個回歸就測不到了。");

            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];
            DeleteProbe.Reset();

            var dataSet = ctx.Repository.GetNewData();
            var master = dataSet.Tables[CrudTestContext.ProgId]!;
            master.Rows[0][SysFields.RowId] = rowId;
            master.Rows[0]["sys_id"] = $"S{runId}";
            master.Rows[0][SysFields.Name] = "待刪除";
            ctx.CreateBo().Save(new SaveArgs { DataSet = dataSet });

            ctx.CreateBo(Resolver(
                    new FormPluginBinding(typeof(BeforeDeleteProbePlugin), PluginStage.BeforeDelete),
                    new FormPluginBinding(typeof(AfterDeleteProbePlugin), PluginStage.AfterDelete)))
                .Delete(new DeleteArgs { RowId = rowId });

            Assert.True(DeleteProbe.BeforeDeleteSawSnapshot);
            Assert.True(DeleteProbe.AfterDeleteSawSnapshot);
            Assert.Equal("待刪除", DeleteProbe.DeletedName);
        }

        // ---- Helpers ----

        private static void DeleteRow(CrudTestContext ctx, Guid rowId)
        {
            try { ctx.Repository.Delete(rowId); } catch (InvalidOperationException) { /* best effort */ }
        }

        private static FixedChainResolver Resolver<T>(PluginStage stage) where T : FormBusinessPlugin
            => Resolver(new FormPluginBinding(typeof(T), stage));

        private static FixedChainResolver Resolver(params FormPluginBinding[] bindings)
            => new FixedChainResolver(FormPluginChain.Create(CrudTestContext.ProgId, bindings));

        private sealed class FixedChainResolver : IFormPluginResolver
        {
            private readonly FormPluginChain _chain;
            public FixedChainResolver(FormPluginChain chain) => _chain = chain;
            public FormPluginChain Resolve(string customizeId, string progId) => _chain;
        }

        // ---- Test plugins ----

        public sealed class RenamingPlugin : FormBusinessPlugin
        {
            public RenamingPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeSave(SaveContext context)
                => context.DataSet.Tables[CrudTestContext.ProgId]!.Rows[0][SysFields.Name] = "BeforeSave 改過";
        }

        public sealed class RejectingPlugin : FormBusinessPlugin
        {
            public RejectingPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeSave(SaveContext context)
                => throw new UserMessageException("擋下這筆。");
        }

        /// <summary>兩個 save 時點探針共用的記錄。</summary>
        public static class TracingProbe
        {
            public static List<string> Calls { get; } = [];
            public static int ConstructedCount { get; set; }
            public static bool AfterSaveHadRefreshedDataSet { get; set; }

            public static void Reset()
            {
                Calls.Clear();
                ConstructedCount = 0;
                AfterSaveHadRefreshedDataSet = false;
            }
        }

        public sealed class BeforeSaveTracingPlugin : FormBusinessPlugin
        {
            public BeforeSaveTracingPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId)
            {
                TracingProbe.ConstructedCount++;
            }

            public override void BeforeSave(SaveContext context) => TracingProbe.Calls.Add("BeforeSave");
        }

        public sealed class AfterSaveTracingPlugin : FormBusinessPlugin
        {
            public AfterSaveTracingPlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId)
            {
                TracingProbe.ConstructedCount++;
            }

            public override void AfterSave(SaveContext context)
            {
                TracingProbe.AfterSaveHadRefreshedDataSet = context.RefreshedDataSet != null;
                TracingProbe.Calls.Add("AfterSave");
            }
        }

        /// <summary>
        /// 兩個 delete 時點探針共用的記錄。一個 plugin 只掛一個時點，所以兩個時點是兩個類別，
        /// 而它們之間沒有可共用的 instance field。
        /// </summary>
        public static class DeleteProbe
        {
            public static bool BeforeDeleteSawSnapshot { get; set; }
            public static bool AfterDeleteSawSnapshot { get; set; }
            public static string DeletedName { get; set; } = string.Empty;

            public static void Reset()
            {
                BeforeDeleteSawSnapshot = false;
                AfterDeleteSawSnapshot = false;
                DeletedName = string.Empty;
            }
        }

        public sealed class BeforeDeleteProbePlugin : FormBusinessPlugin
        {
            public BeforeDeleteProbePlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void BeforeDelete(DeleteContext context)
                => DeleteProbe.BeforeDeleteSawSnapshot = context.Snapshot != null;
        }

        public sealed class AfterDeleteProbePlugin : FormBusinessPlugin
        {
            public AfterDeleteProbePlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public override void AfterDelete(DeleteContext context)
            {
                DeleteProbe.AfterDeleteSawSnapshot = context.Snapshot != null;
                var table = context.Snapshot?.Tables[CrudTestContext.ProgId];
                if (table is { Rows.Count: > 0 })
                    DeleteProbe.DeletedName = table.Rows[0][SysFields.Name]?.ToString() ?? string.Empty;
            }
        }
    }
}
