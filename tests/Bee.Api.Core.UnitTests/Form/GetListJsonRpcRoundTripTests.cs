using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.Form;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Definition.Sorting;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests.Form
{
    /// <summary>
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip：將 <c>Employee.GetList</c>
    /// 透過 executor 派發到 <c>FormBusinessObject.GetList</c>、再由 stub
    /// <c>IDataFormRepository</c> 回傳已知 DataTable，驗證：
    /// <list type="bullet">
    /// <item>action 路由（progId.action 反射查表）正確找到方法</item>
    /// <item>ApiInputConverter（GetListRequest → GetListArgs）保留 Filter / SortFields</item>
    /// <item>ApiOutputConverter（GetListResult → GetListResponse）命名慣例反射有作用</item>
    /// </list>
    /// 不接實體 DB；DB 端的 SQL 行為由 P2 的 <c>FormBusinessObjectGetListTests</c> 覆蓋。
    /// </summary>
    public class GetListJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public GetListJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("Employee.GetList 經 JsonRpcExecutor 應派發到 FormBusinessObject.GetList 並回傳 stub DataTable")]
        public void GetList_ThroughJsonRpc_DispatchesAndReturnsTable()
        {
            // Arrange: 預備 stub repository 與已知 DataTable
            var table = new DataTable("Employee");
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            table.Rows.Add("E001", "員工甲");
            table.Rows.Add("E002", "員工乙");

            var stubRepository = new StubDataFormRepository(table);
            var stubFactory = new StubFormRepositoryFactory(stubRepository);

            var overrideServices = new TestOverrideServiceProvider(
                _fx.Provider,
                (typeof(IFormRepositoryFactory), stubFactory));

            // 自建 BO factory 注入覆寫後的 IServiceProvider —— production BO 透過
            // BeeContext.Services 取 IFormRepositoryFactory 時就會拿到 stub。
            var boFactory = new BusinessObjectFactory(
                overrideServices,
                _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<ISessionInfoService>(),
                _fx.GetRequiredService<IFormBoTypeResolver>());

            var executor = new JsonRpcExecutor(
                boFactory,
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = TestSessionFactory.CreateAccessToken(_fx),
                IsLocalCall = true,
            };

            var rowId = Guid.NewGuid();
            var request = new JsonRpcRequest
            {
                Method = $"Employee.{FormActions.GetList}",
                Params = new JsonRpcParams
                {
                    Value = new GetListRequest
                    {
                        SelectFields = "sys_id,sys_name",
                        Filter = FilterCondition.Equal("sys_rowid", rowId),
                        SortFields = [new SortField("sys_id", SortDirection.Asc)],
                    },
                },
                Id = Guid.NewGuid().ToString(),
            };

            // Act
            var response = executor.Execute(request);

            // Assert: response 成功
            Assert.Null(response.Error);
            var result = Assert.IsType<GetListResponse>(response.Result!.Value);
            Assert.NotNull(result.Table);
            Assert.Equal(2, result.Table!.Rows.Count);
            Assert.Equal("E001", result.Table.Rows[0]["sys_id"]);
            Assert.Equal("員工乙", result.Table.Rows[1]["sys_name"]);

            // Assert: stub 收到的 args 內容（驗證 ApiInputConverter 保留 Filter / SortFields）
            Assert.Equal("sys_id,sys_name", stubRepository.LastSelectFields);
            var condition = Assert.IsType<FilterCondition>(stubRepository.LastFilter);
            Assert.Equal("sys_rowid", condition.FieldName);
            Assert.Equal(rowId, condition.Value);
            Assert.NotNull(stubRepository.LastSortFields);
            Assert.Single(stubRepository.LastSortFields!);
            Assert.Equal("sys_id", stubRepository.LastSortFields![0].FieldName);
        }

        private sealed class StubFormRepositoryFactory : IFormRepositoryFactory
        {
            private readonly IDataFormRepository _data;
            public StubFormRepositoryFactory(IDataFormRepository data) { _data = data; }
            public IDataFormRepository CreateDataFormRepository(string progId) => _data;
            public IReportFormRepository CreateReportFormRepository(string progId)
                => throw new NotSupportedException();
        }

        private sealed class StubDataFormRepository : IDataFormRepository
        {
            private readonly DataTable _table;
            public StubDataFormRepository(DataTable table) { _table = table; }

            public string? LastSelectFields { get; private set; }
            public FilterNode? LastFilter { get; private set; }
            public SortFieldCollection? LastSortFields { get; private set; }

            public DataTable? GetList(
                string selectFields,
                FilterNode? filter,
                SortFieldCollection? sortFields)
            {
                LastSelectFields = selectFields;
                LastFilter = filter;
                LastSortFields = sortFields;
                return _table;
            }
        }
    }
}
