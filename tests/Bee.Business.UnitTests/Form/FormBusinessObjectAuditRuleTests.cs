using System.ComponentModel;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// per-form 稽核規則（<c>st_audit_rule</c>）如何改變 <c>FormBusinessObject</c> 的留痕決定：
    /// 規則 <c>Off</c> 擋掉部署預設開啟的軸、規則 <c>On</c> 打開部署預設關閉的軸、
    /// 敏感旗標流進實際寫出的紀錄，以及規則關掉異動記錄時 delete snapshot 仍須載入。
    /// </summary>
    /// <remarks>
    /// 規則以 stub <see cref="IAuditRuleService"/> 注入而非寫進 <c>st_audit_rule</c>：
    /// 這裡要驗的是 BO 的決策邏輯，不是規則怎麼讀出來的（那由
    /// <c>AuditRuleRepositoryTests</c> 與 <c>AuditRuleServiceTests</c> 負責）。
    /// </remarks>
    public class FormBusinessObjectAuditRuleTests : IClassFixture<SharedDbFixture>
    {
        private const string CompanyId = "AUDITRULE";

        private readonly SharedDbFixture _fx;

        public FormBusinessObjectAuditRuleTests(SharedDbFixture fx) { _fx = fx; }

        private sealed class CapturingAuditLogWriter : IAuditLogWriter
        {
            public List<AuditEntry> Entries { get; } = [];
            public void Write(AuditEntry entry) => Entries.Add(entry);
        }

        private sealed class StubAuditRuleService : IAuditRuleService
        {
            private readonly CompanyAuditRules _rules;
            public StubAuditRuleService(AuditRule rule)
                => _rules = new CompanyAuditRules(CompanyId, [rule]);
            public CompanyAuditRules? Get(string companyId) => _rules;
            public void Remove(string companyId) { }
        }

        /// <summary>
        /// 植入一個帶公司別的 session —— 規則查表以 session 的 CompanyId 為 key，
        /// 沒有公司就等同「查無規則」，那樣就測不到規則本身。
        /// </summary>
        private Guid CreateSessionToken()
        {
            var accessToken = Guid.NewGuid();
            _fx.GetRequiredService<ISessionInfoService>().Set(new SessionInfo
            {
                AccessToken = accessToken,
                UserId = "audit_rule_test",
                UserName = "audit_rule_test",
                CompanyId = CompanyId,
                ExpiredAt = DateTime.UtcNow.AddHours(1),
                ApiEncryptionKey = [],
            });
            return accessToken;
        }

        private static (Type, object?)[] Overrides(
            CapturingAuditLogWriter writer, AuditRule rule,
            bool changeEnabled, bool accessEnabled)
            =>
            [
                (typeof(AuditLogOptions), new AuditLogOptions
                {
                    Enabled = true,
                    ChangeEnabled = changeEnabled,
                    AccessEnabled = accessEnabled,
                }),
                (typeof(IAuditLogWriter), writer),
                (typeof(IAuditRuleService), new StubAuditRuleService(rule)),
            ];

        private static AuditRule Rule(AuditRuleMode change, AuditRuleMode access, bool sensitive = false)
            => new(CrudTestContext.ProgId, change, access, sensitive);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("規則 Off 應擋下 Save 的異動記錄，即使部署預設為開啟")]
        public void Save_RuleOff_WritesNothingDespiteEnabledDefault()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                var dataSet = ctx.Repository.GetNewData();
                var master = dataSet.Tables[CrudTestContext.ProgId]!;
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"F{runId}";
                master.Rows[0][SysFields.Name] = "規則關閉";

                ctx.CreateBoWithSession(CreateSessionToken(), null,
                        Overrides(writer, Rule(AuditRuleMode.Off, AuditRuleMode.Off),
                            changeEnabled: true, accessEnabled: true))
                    .Save(new SaveArgs { DataSet = dataSet });

                Assert.Empty(writer.Entries);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("規則 On 應打開部署預設關閉的異動記錄，且敏感旗標寫進紀錄")]
        public void Save_RuleOn_OverridesDisabledDefaultAndCarriesSensitiveFlag()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                var dataSet = ctx.Repository.GetNewData();
                var master = dataSet.Tables[CrudTestContext.ProgId]!;
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"S{runId}";
                master.Rows[0][SysFields.Name] = "規則開啟";

                // changeEnabled: false 是本測試的重點——規則 On 必須壓過部署預設的關閉，
                // 否則「只記這一張重要表單」這個主要用途就不成立。
                ctx.CreateBoWithSession(CreateSessionToken(), null,
                        Overrides(writer, Rule(AuditRuleMode.On, AuditRuleMode.Off, sensitive: true),
                            changeEnabled: false, accessEnabled: false))
                    .Save(new SaveArgs { DataSet = dataSet });

                var entry = Assert.IsType<ChangeAuditEntry>(Assert.Single(writer.Entries));
                Assert.Equal(ChangeKind.Insert, entry.ChangeKind);
                Assert.True(entry.IsSensitive);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("規則 On 應打開部署預設關閉的檢視記錄")]
        public void GetData_RuleOn_OverridesDisabledDefault()
        {
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                InsertRow(ctx, rowId, $"V{runId}", "規則檢視");

                ctx.CreateBoWithSession(CreateSessionToken(), null,
                        Overrides(writer, Rule(AuditRuleMode.Off, AuditRuleMode.On),
                            changeEnabled: false, accessEnabled: false))
                    .GetData(new GetDataArgs { RowId = rowId });

                var entry = Assert.IsType<AccessAuditEntry>(Assert.Single(writer.Entries));
                Assert.Equal(rowId.ToString(), entry.RowKey);
            }
            finally
            {
                TryDelete(ctx, rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("★規則關掉異動記錄時，delete-stage plugin 仍應拿得到 Snapshot")]
        public void Delete_RuleOff_PluginStillGetsSnapshot()
        {
            // 回歸測試：Snapshot 的載入條件是 `auditChange || pluginNeedsSnapshot || 規則`，
            // 而 per-form 規則現在也能讓 auditChange 變 false。若哪天有人把條件簡化成只看
            // auditChange，同一個 plugin 就會在「有設規則」的部署看到 null、在沒設的看到資料。
            var ctx = new CrudTestContext(_fx, DatabaseType.SQLite);
            var writer = new CapturingAuditLogWriter();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];
            RuleOffSnapshotProbePlugin.Reset();

            InsertRow(ctx, rowId, $"P{runId}", "規則關閉待刪");

            var resolver = new FixedChainResolver(
                FormPluginChain.Create([typeof(RuleOffSnapshotProbePlugin)]));

            ctx.CreateBoWithSession(CreateSessionToken(), resolver,
                    Overrides(writer, Rule(AuditRuleMode.Off, AuditRuleMode.Off),
                        changeEnabled: true, accessEnabled: true))
                .Delete(new DeleteArgs { RowId = rowId });

            Assert.Empty(writer.Entries);
            Assert.True(RuleOffSnapshotProbePlugin.BeforeDeleteSawSnapshot);
            Assert.True(RuleOffSnapshotProbePlugin.AfterDeleteSawSnapshot);
            Assert.Equal("規則關閉待刪", RuleOffSnapshotProbePlugin.DeletedName);
        }

        /// <summary>
        /// 記錄 delete 各階段有沒有拿到 Snapshot。
        /// </summary>
        /// <remarks>
        /// 刻意不共用 <c>FormBusinessObjectPluginIntegrationTests</c> 那個同型探針：
        /// plugin 的狀態是 <c>static</c>，而 xUnit 不同 test class 平行執行，
        /// 共用就會互相覆寫。每個 test class 自帶一份才安全。
        /// </remarks>
        public sealed class RuleOffSnapshotProbePlugin : FormBusinessPlugin
        {
            public RuleOffSnapshotProbePlugin(IBeeContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId) { }

            public static bool BeforeDeleteSawSnapshot { get; private set; }
            public static bool AfterDeleteSawSnapshot { get; private set; }
            public static string DeletedName { get; private set; } = string.Empty;

            public static void Reset()
            {
                BeforeDeleteSawSnapshot = false;
                AfterDeleteSawSnapshot = false;
                DeletedName = string.Empty;
            }

            public override void BeforeDelete(DeleteContext context)
                => BeforeDeleteSawSnapshot = context.Snapshot != null;

            public override void AfterDelete(DeleteContext context)
            {
                AfterDeleteSawSnapshot = context.Snapshot != null;
                var table = context.Snapshot?.Tables[CrudTestContext.ProgId];
                if (table is { Rows.Count: > 0 })
                    DeletedName = table.Rows[0][SysFields.Name]?.ToString() ?? string.Empty;
            }
        }

        private sealed class FixedChainResolver : IFormPluginResolver
        {
            private readonly FormPluginChain _chain;
            public FixedChainResolver(FormPluginChain chain) => _chain = chain;
            public FormPluginChain Resolve(string customizeId, string progId) => _chain;
        }

        private static void InsertRow(CrudTestContext ctx, Guid rowId, string sysId, string sysName)
        {
            var dataSet = ctx.Repository.GetNewData();
            var master = dataSet.Tables[CrudTestContext.ProgId]!;
            master.Rows[0][SysFields.RowId] = rowId;
            master.Rows[0]["sys_id"] = sysId;
            master.Rows[0][SysFields.Name] = sysName;
            ctx.Repository.Save(dataSet);
        }

        private static void TryDelete(CrudTestContext ctx, Guid rowId)
        {
            try { ctx.Repository.Delete(rowId); } catch (InvalidOperationException) { /* best effort */ }
        }
    }
}
