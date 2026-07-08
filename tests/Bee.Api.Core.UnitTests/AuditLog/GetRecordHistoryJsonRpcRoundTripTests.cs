using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.AuditLog;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Logging;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests.AuditLog
{
    /// <summary>
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip：<c>AuditLog.GetRecordHistory</c>
    /// 應經新的 dispatch 分支派發到 <see cref="Bee.Business.AuditLog.LogBusinessObject"/>，由 stub
    /// repository 回傳已知列，驗證 axis 路由 + Input/Output Converter。權限以 fake
    /// IAuthorizationService 放行；不接實體 DB。
    /// </summary>
    public class GetRecordHistoryJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public GetRecordHistoryJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("AuditLog.GetRecordHistory 經 JsonRpcExecutor 應派發到 LogBusinessObject 並回傳結構化 Changes")]
        public void GetRecordHistory_ThroughJsonRpc_DispatchesToLogBo()
        {
            var rowKey = Guid.NewGuid().ToString();
            var logRowId = Guid.NewGuid();
            var table = ChangeTable(logRowId, rowKey);
            var stubFactory = new StubAuditLogRepositoryFactory(new StubAuditLogRepository(table));

            var overrideServices = new TestOverrideServiceProvider(
                _fx.Provider,
                (typeof(IAuthorizationService), new FakeAuth()),
                (typeof(IAuditLogRepositoryFactory), stubFactory));

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

            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.AuditLog}.{LogActions.GetRecordHistory}",
                Params = new JsonRpcParams
                {
                    Value = new GetRecordHistoryRequest { ProgId = "Employee", RowKey = rowKey },
                },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<GetRecordHistoryResponse>(response.Result!.Value);
            Assert.Equal("Employee", result.ProgId);
            Assert.Equal(rowKey, result.RowKey);
            var entry = Assert.Single(result.Changes);
            Assert.Equal(logRowId, entry.SysRowId);
            Assert.Equal(ChangeKind.Insert, entry.ChangeKind);
        }

        private static DataTable ChangeTable(Guid logRowId, string rowKey)
        {
            var table = new DataTable("st_log_change");
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("log_time", typeof(DateTime));
            table.Columns.Add("user_id", typeof(string));
            table.Columns.Add("user_name", typeof(string));
            table.Columns.Add("change_kind", typeof(int));
            table.Columns.Add("is_sensitive", typeof(bool));
            table.Columns.Add("source", typeof(string));
            table.Columns.Add("changes_xml", typeof(string));
            // Minimal (non-DiffGram) payload: the event header still restores; Fields is empty.
            table.Rows.Add(logRowId, new DateTime(2026, 7, 8, 3, 0, 0, DateTimeKind.Utc),
                "demo", "Demo User", (int)ChangeKind.Insert, false, "Employee.Save", string.Empty);
            return table;
        }

        private sealed class FakeAuth : IAuthorizationService
        {
            public bool Can(Guid accessToken, string modelId, PermissionAction action) => true;
        }

        private sealed class StubAuditLogRepository : IAuditLogRepository
        {
            private readonly DataTable _table;
            public StubAuditLogRepository(DataTable table) { _table = table; }
            public DataTable GetRecordChangeHistory(string progId, string rowKey, string? companyId) => _table;
        }

        private sealed class StubAuditLogRepositoryFactory : IAuditLogRepositoryFactory
        {
            private readonly IAuditLogRepository _repo;
            public StubAuditLogRepositoryFactory(IAuditLogRepository repo) { _repo = repo; }
            public IAuditLogRepository CreateAuditLogRepository() => _repo;
        }
    }
}
