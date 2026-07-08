using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.AuditLog;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Logging;
using Bee.Definition.Paging;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests.AuditLog
{
    /// <summary>
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip：<c>AuditLog.*</c> 三個 action 應經
    /// dispatch 分支派發到 <see cref="Bee.Business.AuditLog.LogBusinessObject"/>，由 stub repository
    /// 回傳已知資料，驗證 axis 路由 + Input/Output Converter。權限以 fake IAuthorizationService 放行；
    /// 不接實體 DB。
    /// </summary>
    public class AuditLogJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public AuditLogJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        private JsonRpcResponse Dispatch(StubAuditLogRepository repo, string action, object request)
        {
            var overrideServices = new TestOverrideServiceProvider(
                _fx.Provider,
                (typeof(IAuthorizationService), new FakeAuth()),
                (typeof(IAuditLogRepositoryFactory), new StubAuditLogRepositoryFactory(repo)));

            var boFactory = new BusinessObjectFactory(
                overrideServices,
                _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<ISessionInfoService>(),
                _fx.GetRequiredService<ILanguageService>(),
                _fx.GetRequiredService<IFormBoTypeResolver>());

            var executor = new JsonRpcExecutor(
                boFactory,
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = TestSessionFactory.CreateAccessToken(_fx),
                IsLocalCall = true,
            };

            return executor.Execute(new JsonRpcRequest
            {
                Method = $"{SysProgIds.AuditLog}.{action}",
                Params = new JsonRpcParams { Value = request },
                Id = Guid.NewGuid().ToString(),
            });
        }

        [Fact]
        [DisplayName("AuditLog.GetChangeLog 經 executor 應派發並回標頭 DataTable + 分頁")]
        public void GetChangeLog_ThroughJsonRpc_Dispatches()
        {
            var repo = new StubAuditLogRepository(HeaderPage(2), null);
            var response = Dispatch(repo, LogActions.GetChangeLog,
                new GetChangeLogRequest { ProgId = "Employee", ChangeKind = ChangeKind.Update });

            Assert.Null(response.Error);
            var result = Assert.IsType<GetChangeLogResponse>(response.Result!.Value);
            Assert.Equal(2, result.Table!.Rows.Count);
            Assert.NotNull(result.Paging);
        }

        [Fact]
        [DisplayName("AuditLog.GetChangeDetail 經 executor 應派發並回還原後的 Fields")]
        public void GetChangeDetail_ThroughJsonRpc_Dispatches()
        {
            var sysRowId = Guid.NewGuid();
            var repo = new StubAuditLogRepository(HeaderPage(0), DetailRow(sysRowId));
            var response = Dispatch(repo, LogActions.GetChangeDetail,
                new GetChangeDetailRequest { SysRowId = sysRowId });

            Assert.Null(response.Error);
            var result = Assert.IsType<GetChangeDetailResponse>(response.Result!.Value);
            Assert.Equal(sysRowId, result.SysRowId);
            Assert.Equal(ChangeKind.Insert, result.ChangeKind);
        }

        [Theory]
        [InlineData("GetLoginLog")]
        [InlineData("GetAccessLog")]
        [InlineData("GetApiAnomalyLog")]
        [InlineData("GetDbAnomalyLog")]
        [DisplayName("AuditLog 各清單 action 經 executor 應派發並回 LogListResponse")]
        public void ListActions_ThroughJsonRpc_ReturnLogListResponse(string action)
        {
            var repo = new StubAuditLogRepository(HeaderPage(2), null);
            object request = action switch
            {
                "GetLoginLog" => new GetLoginLogRequest { UserId = "demo", Event = LoginEvent.LoginFailed },
                "GetAccessLog" => new GetAccessLogRequest { ProgId = "Order" },
                "GetApiAnomalyLog" => new GetApiAnomalyLogRequest { Kind = AnomalyKind.Slow },
                _ => new GetDbAnomalyLogRequest { DatabaseId = "company", Kind = AnomalyKind.Timeout },
            };

            var response = Dispatch(repo, action, request);

            Assert.Null(response.Error);
            var result = Assert.IsType<LogListResponse>(response.Result!.Value);
            Assert.Equal(2, result.Table!.Rows.Count);
            Assert.NotNull(result.Paging);
        }

        [Theory]
        [InlineData("GetApiAnomalySummary")]
        [InlineData("GetDbAnomalySummary")]
        [InlineData("GetTopApiMethods")]
        [DisplayName("AuditLog 各聚合 action 經 executor 應派發並回 LogAggregateResponse")]
        public void AggregateActions_ThroughJsonRpc_ReturnLogAggregateResponse(string action)
        {
            var repo = new StubAuditLogRepository(HeaderPage(0), null);
            object request = action switch
            {
                "GetApiAnomalySummary" => new GetApiAnomalySummaryRequest(),
                "GetDbAnomalySummary" => new GetDbAnomalySummaryRequest(),
                _ => new GetTopApiMethodsRequest { TopN = 5 },
            };

            var response = Dispatch(repo, action, request);

            Assert.Null(response.Error);
            var result = Assert.IsType<LogAggregateResponse>(response.Result!.Value);
            Assert.NotNull(result.Table);
            Assert.Single(result.Table!.Rows);
        }

        private static DataTable HeaderTable(bool withChangesXml = false)
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
            if (withChangesXml) { t.Columns.Add("changes_xml", typeof(string)); }
            return t;
        }

        private static AuditLogPage HeaderPage(int rows)
        {
            var t = HeaderTable();
            for (int i = 0; i < rows; i++)
            {
                t.Rows.Add(Guid.NewGuid(), new DateTime(2026, 7, 8, 3, 0, 0, DateTimeKind.Utc),
                    "demo", "Demo User", "c1", "Company One", "Employee", Guid.NewGuid().ToString(),
                    (int)ChangeKind.Update, false, "Employee.Save");
            }
            return new AuditLogPage { Table = t, Paging = new PagingInfo { Page = 1, PageSize = 50 } };
        }

        private static DataTable DetailRow(Guid sysRowId)
        {
            var t = HeaderTable(withChangesXml: true);
            // Empty (non-DiffGram) payload: the header still maps; Fields is empty.
            t.Rows.Add(sysRowId, new DateTime(2026, 7, 8, 3, 0, 0, DateTimeKind.Utc),
                "demo", "Demo User", "c1", "Company One", "Employee", "R-1",
                (int)ChangeKind.Insert, false, "Employee.Save", string.Empty);
            return t;
        }

        private sealed class FakeAuth : IAuthorizationService
        {
            public bool Can(Guid accessToken, string modelId, PermissionAction action) => true;
        }

        private sealed class StubAuditLogRepository : IAuditLogRepository
        {
            private readonly AuditLogPage _page;
            private readonly DataTable? _detail;
            public StubAuditLogRepository(AuditLogPage page, DataTable? detail) { _page = page; _detail = detail; }
            public AuditLogPage GetChangeLog(ChangeLogQuery query, PagingOptions paging) => _page;
            public DataTable? GetChangeById(Guid sysRowId, string? companyId) => _detail;
            public AuditLogPage GetLoginLog(LoginLogQuery query, PagingOptions paging) => _page;
            public AuditLogPage GetAccessLog(AccessLogQuery query, PagingOptions paging) => _page;
            public AuditLogPage GetApiAnomalyLog(ApiAnomalyLogQuery query, PagingOptions paging) => _page;
            public AuditLogPage GetDbAnomalyLog(DbAnomalyLogQuery query, PagingOptions paging) => _page;

            private static DataTable Agg() { var t = new DataTable("agg"); t.Columns.Add("anomaly_kind", typeof(int)); t.Rows.Add((int)AnomalyKind.Slow); return t; }
            public DataTable GetApiAnomalySummary(DateTime? fromUtc, DateTime? toUtc, string? companyId) => Agg();
            public DataTable GetDbAnomalySummary(DateTime? fromUtc, DateTime? toUtc) => Agg();
            public DataTable GetTopApiMethods(DateTime? fromUtc, DateTime? toUtc, int topN, string? companyId) => Agg();
        }

        private sealed class StubAuditLogRepositoryFactory : IAuditLogRepositoryFactory
        {
            private readonly IAuditLogRepository _repo;
            public StubAuditLogRepositoryFactory(IAuditLogRepository repo) { _repo = repo; }
            public IAuditLogRepository CreateAuditLogRepository() => _repo;
        }
    }
}
