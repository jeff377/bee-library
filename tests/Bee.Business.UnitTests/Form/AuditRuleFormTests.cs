using System.ComponentModel;
using Bee.Business.AuditLog;
using Bee.Business.Form;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Form;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// 稽核規則維護表單本身（<c>AuditRule</c> / <c>st_audit_rule</c>）：政策變更一律留痕且標敏感，
    /// 且存檔後會清掉該公司的規則快取。
    /// </summary>
    public class AuditRuleFormTests : IClassFixture<SharedDbFixture>
    {
        private const string CompanyId = "AUDITFORM";

        private readonly SharedDbFixture _fx;
        private readonly IDataFormRepository _repository;

        public AuditRuleFormTests(SharedDbFixture fx)
        {
            _fx = fx;
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.SQLite, "company");
            var defineAccess = fx.GetRequiredService<IDefineAccess>();
            _repository = new DataFormRepository(
                TestRepositoryContext.Create(
                    fx.GetRequiredService<IDbConnectionManager>(),
                    defineAccess: defineAccess,
                    dbAccessFactory: fx.GetRequiredService<IDbAccessFactory>()),
                SysProgIds.AuditRule,
                defineAccess.GetFormSchema(SysProgIds.AuditRule),
                databaseId);
        }

        private sealed class CapturingAuditLogWriter : IAuditLogWriter
        {
            public List<AuditEntry> Entries { get; } = [];
            public void Write(AuditEntry entry) => Entries.Add(entry);
        }

        /// <summary>
        /// 回報 Remove 有沒有被呼叫，並依測試指定的規則回答查詢。
        /// </summary>
        private sealed class RecordingAuditRuleService : IAuditRuleService
        {
            private readonly CompanyAuditRules _rules;
            public RecordingAuditRuleService(AuditRule rule)
                => _rules = new CompanyAuditRules(CompanyId, [rule]);
            public List<string> Removed { get; } = [];
            public CompanyAuditRules? Get(string companyId) => _rules;
            public void Remove(string companyId) => Removed.Add(companyId);
        }

        /// <summary>
        /// 放行所有動作。維護表單宣告了 PermissionModelId，enforcement 是 fail-closed——
        /// 少了這個 fake，兩個測試都會停在 ForbiddenException 而測不到後面的事。
        /// 這個必要性本身就是「政策表單確實受權限把關」的旁證。
        /// </summary>
        private sealed class AllowAllAuthorization : ICompanyAuthorizationService
        {
            public bool Can(Guid accessToken, string modelId, PermissionAction action) => true;
        }

        private sealed class StubFactory : IRepositoryFactory
        {
            private readonly IDataFormRepository _repository;
            private readonly IAuditRuleRepository? _auditRules;
            public StubFactory(IDataFormRepository repository, IAuditRuleRepository? auditRules)
            {
                _repository = repository;
                _auditRules = auditRules;
            }
            public T CreateFormRepository<T>(Guid accessToken, string progId) where T : class, IDataFormRepository
                => (T)_repository;
            public T Create<T>(Guid accessToken = default) where T : class
                => _auditRules as T ?? throw new NotSupportedException();
        }

        /// <summary>不做事的通知端：本測試驗的是留痕與快取清除，不是跨節點公告。</summary>
        private sealed class NoOpAuditRuleRepository : IAuditRuleRepository
        {
            public List<string> Notified { get; } = [];
            public IReadOnlyList<AuditRule> GetRules(string databaseId) => [];
            public void NotifyRulesChanged(string companyId) => Notified.Add(companyId);
        }

        private Guid CreateSessionToken()
        {
            var accessToken = Guid.NewGuid();
            _fx.GetRequiredService<ISessionInfoService>().Set(new SessionInfo
            {
                AccessToken = accessToken,
                UserId = "audit_form_test",
                UserName = "audit_form_test",
                CompanyId = CompanyId,
                ExpiredAt = DateTime.UtcNow.AddHours(1),
                ApiEncryptionKey = [],
            });
            return accessToken;
        }

        private AuditRuleBusinessObject CreateBo(
            Guid accessToken, CapturingAuditLogWriter writer,
            IAuditRuleService ruleService, IAuditRuleRepository auditRules)
        {
            var ctx = TestBeeContext.CreateWithOverrides(_fx,
                (typeof(IRepositoryFactory), new StubFactory(_repository, auditRules)),
                (typeof(AuditLogOptions), new AuditLogOptions
                {
                    Enabled = true,
                    // 兩軸的部署預設都關掉：接下來寫出的任何紀錄都只能來自豁免。
                    ChangeEnabled = false,
                    AccessEnabled = false,
                }),
                (typeof(IAuditLogWriter), writer),
                (typeof(IAuditRuleService), ruleService),
                (typeof(ICompanyAuthorizationService), new AllowAllAuthorization()));
            return new AuditRuleBusinessObject(ctx, accessToken, SysProgIds.AuditRule);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("★政策表單不受規則表擺布：規則說 Off、部署預設也關，仍留痕且標敏感")]
        public void Save_PolicyFormIsExemptFromItsOwnRule()
        {
            // 「稽核不可被稽核政策關掉」的回歸測試。若沒有這道豁免，任何能維護規則的人
            // 只要把 AuditRule 這一列設成 Off，之後所有政策變更都無痕——整套稽核可以
            // 被自己靜靜關掉，且沒有任何紀錄顯示發生過。
            var writer = new CapturingAuditLogWriter();
            var ruleService = new RecordingAuditRuleService(
                new AuditRule(SysProgIds.AuditRule, AuditRuleMode.Off, AuditRuleMode.Off, false));
            var auditRules = new NoOpAuditRuleRepository();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                var dataSet = _repository.GetNewData();
                var master = dataSet.Tables[SysProgIds.AuditRule]!;
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"P{runId}";
                master.Rows[0][SysFields.Name] = "受稽核的表單";
                master.Rows[0]["change_mode"] = (int)AuditRuleMode.On;
                master.Rows[0]["access_mode"] = (int)AuditRuleMode.Inherit;

                CreateBo(CreateSessionToken(), writer, ruleService, auditRules)
                    .Save(new SaveArgs { DataSet = dataSet });

                var entry = Assert.IsType<ChangeAuditEntry>(Assert.Single(writer.Entries));
                Assert.Equal(ChangeKind.Insert, entry.ChangeKind);
                Assert.Equal(SysProgIds.AuditRule, entry.ProgId);
                // 政策變更一律屬敏感，等同 SystemBusinessObject 對「授予能力」那類操作的處置。
                Assert.True(entry.IsSensitive);
            }
            finally
            {
                TryDelete(rowId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("存檔後應清掉該公司的規則快取並發出跨節點公告")]
        public void Save_InvalidatesCompanyRuleCache()
        {
            var writer = new CapturingAuditLogWriter();
            var ruleService = new RecordingAuditRuleService(
                new AuditRule("Other", AuditRuleMode.Inherit, AuditRuleMode.Inherit, false));
            var auditRules = new NoOpAuditRuleRepository();
            var rowId = Guid.NewGuid();
            string runId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                var dataSet = _repository.GetNewData();
                var master = dataSet.Tables[SysProgIds.AuditRule]!;
                master.Rows[0][SysFields.RowId] = rowId;
                master.Rows[0]["sys_id"] = $"I{runId}";
                master.Rows[0][SysFields.Name] = "快取失效";

                CreateBo(CreateSessionToken(), writer, ruleService, auditRules)
                    .Save(new SaveArgs { DataSet = dataSet });

                // 少了本機清除，操作者會看到自己剛改的規則毫無作用——快照沒有到期時間。
                Assert.Equal([CompanyId], ruleService.Removed);
                Assert.Equal([CompanyId], auditRules.Notified);
            }
            finally
            {
                TryDelete(rowId);
            }
        }

        private void TryDelete(Guid rowId)
        {
            try { _repository.Delete(rowId); } catch (InvalidOperationException) { /* best effort */ }
        }
    }
}
