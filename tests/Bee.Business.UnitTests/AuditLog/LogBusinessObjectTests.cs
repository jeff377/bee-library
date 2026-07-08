using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Business.AuditLog;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Paging;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.AuditLog
{
    /// <summary>
    /// <see cref="LogBusinessObject"/> 的 BO 層行為測試（stub repository，不接實體 DB）：
    /// 清單方法（<c>GetChangeLog</c> 等）回標頭 + 分頁、filter 透傳；
    /// 明細方法（<c>GetChangeDetail</c>）把 changes_xml DiffGram 還原為結構化 before/after、
    /// 查無資料丟例外；三方法皆有權限 gate 與參數驗證。
    /// </summary>
    public class LogBusinessObjectTests : IClassFixture<BeeTestFixture>
    {
        private const string ProgId = "Employee";
        private readonly BeeTestFixture _fx;

        public LogBusinessObjectTests(BeeTestFixture fx) { _fx = fx; }

        private LogBusinessObject Bo(StubAuditLogRepository repo, bool authorized = true)
        {
            var ctx = TestBeeContext.CreateWithOverrides(_fx,
                (typeof(IAuthorizationService), new FakeAuth(authorized)),
                (typeof(IAuditLogRepositoryFactory), new StubAuditLogRepositoryFactory(repo)));
            return new LogBusinessObject(ctx, Guid.NewGuid());
        }

        // ---- GetChangeLog (filtered list) ----

        [Fact]
        [DisplayName("GetChangeLog 應回標頭清單 + 分頁，並把 typed filter 透傳給 repository")]
        public void GetChangeLog_Authorized_PassesFilter()
        {
            var repo = new StubAuditLogRepository(HeaderPage(2));
            var bo = Bo(repo);
            var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = bo.GetChangeLog(new GetChangeLogArgs
            {
                FromUtc = from,
                UserId = "demo",
                ChangeKind = ChangeKind.Update,
                Paging = new PagingOptions { PageSize = 10 },
            });

            Assert.NotNull(result.Table);
            Assert.Equal(2, result.Table!.Rows.Count);
            Assert.Equal(from, repo.LastQuery!.FromUtc);
            Assert.Equal("demo", repo.LastQuery.UserId);
            Assert.Equal(ChangeKind.Update, repo.LastQuery.ChangeKind);
            Assert.Equal(10, repo.LastPaging!.PageSize);
        }

        [Fact]
        [DisplayName("GetChangeLog 未授權應丟 UnauthorizedAccessException")]
        public void GetChangeLog_NotAuthorized_Throws()
        {
            var bo = Bo(new StubAuditLogRepository(HeaderPage(0)), authorized: false);
            Assert.Throws<UnauthorizedAccessException>(() => bo.GetChangeLog(new GetChangeLogArgs()));
        }

        // ---- GetChangeDetail (restore one event) ----

        [Fact]
        [DisplayName("GetChangeDetail 應把單筆 changes_xml DiffGram 還原為欄位級 before/after")]
        public void GetChangeDetail_Authorized_RestoresFields()
        {
            var sysRowId = Guid.NewGuid();
            var rowKey = Guid.NewGuid().ToString();
            var xml = BuildModifyDiffGram("st_employee", rowKey, "name", "Alice", "Alice Wang");
            var repo = new StubAuditLogRepository(HeaderPage(0), DetailRow(sysRowId, rowKey, ChangeKind.Update, xml));
            var bo = Bo(repo);

            var result = bo.GetChangeDetail(new GetChangeDetailArgs { SysRowId = sysRowId });

            Assert.Equal(sysRowId, result.SysRowId);
            Assert.Equal(ChangeKind.Update, result.ChangeKind);
            Assert.Equal(ProgId, result.ProgId);
            Assert.Equal(rowKey, result.RowKey);
            var field = Assert.Single(result.Fields);
            Assert.Equal("name", field.FieldName);
            Assert.Equal("Alice", field.OldValue);
            Assert.Equal("Alice Wang", field.NewValue);
            Assert.Equal(sysRowId, repo.LastDetailId);
        }

        [Fact]
        [DisplayName("GetChangeDetail 查無資料應丟 InvalidOperationException")]
        public void GetChangeDetail_NotFound_Throws()
        {
            var repo = new StubAuditLogRepository(HeaderPage(0), detail: null);
            var bo = Bo(repo);
            Assert.Throws<InvalidOperationException>(() =>
                bo.GetChangeDetail(new GetChangeDetailArgs { SysRowId = Guid.NewGuid() }));
        }

        [Fact]
        [DisplayName("GetChangeDetail 缺 SysRowId 應丟 ArgumentException")]
        public void GetChangeDetail_EmptySysRowId_Throws()
        {
            var bo = Bo(new StubAuditLogRepository(HeaderPage(0)));
            Assert.Throws<ArgumentException>(() =>
                bo.GetChangeDetail(new GetChangeDetailArgs { SysRowId = Guid.Empty }));
        }

        [Fact]
        [DisplayName("GetChangeDetail 未授權應丟 UnauthorizedAccessException")]
        public void GetChangeDetail_NotAuthorized_Throws()
        {
            var bo = Bo(new StubAuditLogRepository(HeaderPage(0)), authorized: false);
            Assert.Throws<UnauthorizedAccessException>(() =>
                bo.GetChangeDetail(new GetChangeDetailArgs { SysRowId = Guid.NewGuid() }));
        }

        // ---- login / access / anomaly lists ----

        [Fact]
        [DisplayName("GetLoginLog 應回清單 + 分頁，並透傳 event / user filter")]
        public void GetLoginLog_Authorized_PassesFilter()
        {
            var repo = new StubAuditLogRepository(HeaderPage(2));
            var result = Bo(repo).GetLoginLog(new GetLoginLogArgs { UserId = "demo", Event = LoginEvent.LoginFailed });

            Assert.Equal(2, result.Table!.Rows.Count);
            var q = Assert.IsType<LoginLogQuery>(repo.LastListQuery);
            Assert.Equal("demo", q.UserId);
            Assert.Equal(LoginEvent.LoginFailed, q.Event);
        }

        [Fact]
        [DisplayName("GetAccessLog 應回清單 + 分頁，並透傳 progId / rowKey filter")]
        public void GetAccessLog_Authorized_PassesFilter()
        {
            var repo = new StubAuditLogRepository(HeaderPage(1));
            var result = Bo(repo).GetAccessLog(new GetAccessLogArgs { ProgId = "Order", RowKey = "R-9" });

            Assert.Single(result.Table!.Rows);
            var q = Assert.IsType<AccessLogQuery>(repo.LastListQuery);
            Assert.Equal("Order", q.ProgId);
            Assert.Equal("R-9", q.RowKey);
        }

        [Fact]
        [DisplayName("GetApiAnomalyLog 應回清單，並透傳 method / kind filter")]
        public void GetApiAnomalyLog_Authorized_PassesFilter()
        {
            var repo = new StubAuditLogRepository(HeaderPage(3));
            var result = Bo(repo).GetApiAnomalyLog(new GetApiAnomalyLogArgs { Method = "Order.Save", Kind = AnomalyKind.Slow });

            Assert.Equal(3, result.Table!.Rows.Count);
            var q = Assert.IsType<ApiAnomalyLogQuery>(repo.LastListQuery);
            Assert.Equal("Order.Save", q.Method);
            Assert.Equal(AnomalyKind.Slow, q.Kind);
        }

        [Fact]
        [DisplayName("GetDbAnomalyLog 應回清單，並透傳 databaseId / kind filter")]
        public void GetDbAnomalyLog_Authorized_PassesFilter()
        {
            var repo = new StubAuditLogRepository(HeaderPage(1));
            var result = Bo(repo).GetDbAnomalyLog(new GetDbAnomalyLogArgs { DatabaseId = "company", Kind = AnomalyKind.Timeout });

            Assert.Single(result.Table!.Rows);
            var q = Assert.IsType<DbAnomalyLogQuery>(repo.LastListQuery);
            Assert.Equal("company", q.DatabaseId);
            Assert.Equal(AnomalyKind.Timeout, q.Kind);
        }

        [Fact]
        [DisplayName("清單方法未授權應丟 UnauthorizedAccessException")]
        public void ListMethods_NotAuthorized_Throw()
        {
            var bo = Bo(new StubAuditLogRepository(HeaderPage(0)), authorized: false);
            Assert.Throws<UnauthorizedAccessException>(() => bo.GetLoginLog(new GetLoginLogArgs()));
            Assert.Throws<UnauthorizedAccessException>(() => bo.GetAccessLog(new GetAccessLogArgs()));
            Assert.Throws<UnauthorizedAccessException>(() => bo.GetApiAnomalyLog(new GetApiAnomalyLogArgs()));
            Assert.Throws<UnauthorizedAccessException>(() => bo.GetDbAnomalyLog(new GetDbAnomalyLogArgs()));
        }

        // ---- anomaly aggregates (Phase 3a) ----

        [Fact]
        [DisplayName("GetApiAnomalySummary 應回聚合 Table")]
        public void GetApiAnomalySummary_Authorized_ReturnsTable()
        {
            var repo = new StubAuditLogRepository(HeaderPage(0));
            var result = Bo(repo).GetApiAnomalySummary(new GetApiAnomalySummaryArgs());
            Assert.Same(repo.AggregateResult, result.Table);
        }

        [Fact]
        [DisplayName("GetDbAnomalySummary 應走 DB 聚合（無 company scope）")]
        public void GetDbAnomalySummary_Authorized_UsesDbSummary()
        {
            var repo = new StubAuditLogRepository(HeaderPage(0));
            var result = Bo(repo).GetDbAnomalySummary(new GetDbAnomalySummaryArgs());
            Assert.Same(repo.AggregateResult, result.Table);
            Assert.True(repo.DbSummaryCalled);
        }

        [Fact]
        [DisplayName("GetTopApiMethods 應把 TopN 透傳給 repository")]
        public void GetTopApiMethods_Authorized_PassesTopN()
        {
            var repo = new StubAuditLogRepository(HeaderPage(0));
            var result = Bo(repo).GetTopApiMethods(new GetTopApiMethodsArgs { TopN = 7 });
            Assert.Same(repo.AggregateResult, result.Table);
            Assert.Equal(7, repo.LastTopN);
        }

        [Fact]
        [DisplayName("聚合方法未授權應丟 UnauthorizedAccessException")]
        public void AggregateMethods_NotAuthorized_Throw()
        {
            var bo = Bo(new StubAuditLogRepository(HeaderPage(0)), authorized: false);
            Assert.Throws<UnauthorizedAccessException>(() => bo.GetApiAnomalySummary(new GetApiAnomalySummaryArgs()));
            Assert.Throws<UnauthorizedAccessException>(() => bo.GetDbAnomalySummary(new GetDbAnomalySummaryArgs()));
            Assert.Throws<UnauthorizedAccessException>(() => bo.GetTopApiMethods(new GetTopApiMethodsArgs()));
        }

        // ---- helpers ----

        private static DataTable HeaderTable()
        {
            var t = new DataTable("st_log_change");
            t.Columns.Add("sys_rowid", typeof(Guid));
            t.Columns.Add("log_time", typeof(DateTime));
            t.Columns.Add("user_id", typeof(string));
            t.Columns.Add("user_name", typeof(string));
            t.Columns.Add("company_id", typeof(string));
            t.Columns.Add("company_name", typeof(string));
            t.Columns.Add("prog_id", typeof(string));
            t.Columns.Add("row_key", typeof(string));
            t.Columns.Add("change_kind", typeof(int));
            t.Columns.Add("is_sensitive", typeof(bool));
            t.Columns.Add("source", typeof(string));
            return t;
        }

        private static AuditLogPage HeaderPage(int rows)
        {
            var t = HeaderTable();
            var logTime = new DateTime(2026, 7, 8, 3, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < rows; i++)
            {
                t.Rows.Add(Guid.NewGuid(), logTime, "demo", "Demo User", "c1", "Company One",
                    ProgId, Guid.NewGuid().ToString(), (int)ChangeKind.Update, false, ProgId + ".Save");
            }
            return new AuditLogPage { Table = t, Paging = new PagingInfo { Page = 1, PageSize = 50, HasMore = false } };
        }

        private static DataTable DetailRow(Guid sysRowId, string rowKey, ChangeKind kind, string changesXml)
        {
            var t = HeaderTable();
            t.Columns.Add("changes_xml", typeof(string));
            t.Rows.Add(sysRowId, new DateTime(2026, 7, 8, 3, 0, 0, DateTimeKind.Utc), "demo", "Demo User",
                "c1", "Company One", ProgId, rowKey, (int)kind, false, ProgId + ".Save", changesXml);
            return t;
        }

        private static string BuildModifyDiffGram(string tableName, string rowKey, string column, string oldValue, string newValue)
        {
            var ds = new DataSet("Root");
            var table = ds.Tables.Add(tableName);
            table.Columns.Add("sys_rowid", typeof(string));
            table.Columns.Add(column, typeof(string));
            var row = table.Rows.Add(rowKey, oldValue);
            ds.AcceptChanges();
            row[column] = newValue;
            using var changes = ds.GetChanges()!;
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            changes.WriteXml(writer, XmlWriteMode.DiffGram);
            return writer.ToString();
        }

        private sealed class FakeAuth : IAuthorizationService
        {
            private readonly bool _allowed;
            public FakeAuth(bool allowed) { _allowed = allowed; }
            public bool Can(Guid accessToken, string modelId, PermissionAction action) => _allowed;
        }

        private sealed class StubAuditLogRepository : IAuditLogRepository
        {
            private readonly AuditLogPage _page;
            private readonly DataTable? _detail;
            public ChangeLogQuery? LastQuery { get; private set; }
            public PagingOptions? LastPaging { get; private set; }
            public Guid? LastDetailId { get; private set; }
            public object? LastListQuery { get; private set; }

            public StubAuditLogRepository(AuditLogPage page, DataTable? detail = null)
            {
                _page = page;
                _detail = detail;
            }

            public AuditLogPage GetChangeLog(ChangeLogQuery query, PagingOptions paging)
            {
                LastQuery = query;
                LastPaging = paging;
                return _page;
            }

            public DataTable? GetChangeById(Guid sysRowId, string? companyId)
            {
                LastDetailId = sysRowId;
                return _detail;
            }

            public AuditLogPage GetLoginLog(LoginLogQuery query, PagingOptions paging) { LastListQuery = query; LastPaging = paging; return _page; }
            public AuditLogPage GetAccessLog(AccessLogQuery query, PagingOptions paging) { LastListQuery = query; LastPaging = paging; return _page; }
            public AuditLogPage GetApiAnomalyLog(ApiAnomalyLogQuery query, PagingOptions paging) { LastListQuery = query; LastPaging = paging; return _page; }
            public AuditLogPage GetDbAnomalyLog(DbAnomalyLogQuery query, PagingOptions paging) { LastListQuery = query; LastPaging = paging; return _page; }

            public DataTable AggregateResult { get; } = new DataTable("agg");
            public int? LastTopN { get; private set; }
            public bool DbSummaryCalled { get; private set; }
            public DataTable GetApiAnomalySummary(DateTime? fromUtc, DateTime? toUtc, string? companyId) => AggregateResult;
            public DataTable GetDbAnomalySummary(DateTime? fromUtc, DateTime? toUtc) { DbSummaryCalled = true; return AggregateResult; }
            public DataTable GetTopApiMethods(DateTime? fromUtc, DateTime? toUtc, int topN, string? companyId) { LastTopN = topN; return AggregateResult; }
        }

        private sealed class StubAuditLogRepositoryFactory : IAuditLogRepositoryFactory
        {
            private readonly IAuditLogRepository _repo;
            public StubAuditLogRepositoryFactory(IAuditLogRepository repo) { _repo = repo; }
            public IAuditLogRepository CreateAuditLogRepository() => _repo;
        }
    }
}
