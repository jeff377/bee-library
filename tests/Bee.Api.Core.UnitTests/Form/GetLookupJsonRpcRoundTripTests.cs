using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.Form;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Paging;
using Bee.Definition.Security;
using Bee.Definition.Sorting;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests.Form
{
    /// <summary>
    /// 走 <see cref="JsonRpcExecutor"/> 的 round-trip：將 <c>Employee.GetLookup</c> 派發到
    /// <c>FormBusinessObject.GetLookup</c>、由 stub <c>IDataFormRepository</c> 回傳已知
    /// DataTable，驗證 server 端的 lookup 欄位集解析（Employee 未宣告 LookupFields →
    /// 預設 <c>sys_rowid,sys_id,sys_name</c>）、SearchText 組出的 OR Contains 過濾、
    /// 與未帶 Paging 時的預設分頁。
    /// </summary>
    public class GetLookupJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public GetLookupJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("Employee.GetLookup 經 JsonRpcExecutor 應以預設欄位集查詢並回傳 stub DataTable")]
        public void GetLookup_ThroughJsonRpc_UsesDefaultLookupFieldSet()
        {
            var table = new DataTable("Employee");
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            table.Rows.Add(Guid.NewGuid(), "E001", "員工甲");

            var stubRepository = new StubDataFormRepository(table);
            var executor = CreateExecutor(stubRepository);

            var request = new JsonRpcRequest
            {
                Method = $"Employee.{FormActions.GetLookup}",
                Params = new JsonRpcParams
                {
                    Value = new GetLookupRequest { SearchText = "甲" },
                },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<GetLookupResponse>(response.Result!.Value);
            Assert.NotNull(result.Table);
            Assert.Single(result.Table!.Rows);
            Assert.Equal("E001", result.Table.Rows[0]["sys_id"]);

            // Server-resolved projection: default lookup set prefixed with sys_rowid.
            Assert.Equal("sys_rowid,sys_id,sys_name", stubRepository.LastSelectFields);

            // SearchText becomes an OR group of Contains over the string-typed lookup fields.
            var group = Assert.IsType<FilterGroup>(stubRepository.LastFilter);
            Assert.Equal(LogicalOperator.Or, group.Operator);
            Assert.Equal(2, group.Nodes.Count);
            var first = Assert.IsType<FilterCondition>(group.Nodes[0]);
            Assert.Equal("sys_id", first.FieldName);
            Assert.Equal(ComparisonOperator.Contains, first.Operator);
            Assert.Equal("甲", first.Value);
            var second = Assert.IsType<FilterCondition>(group.Nodes[1]);
            Assert.Equal("sys_name", second.FieldName);

            // Omitted paging falls back to the server default page size.
            Assert.NotNull(stubRepository.LastPaging);
            Assert.Equal(1, stubRepository.LastPaging!.Page);
            Assert.Equal(100, stubRepository.LastPaging.PageSize);
        }

        [Fact]
        [DisplayName("Employee.GetLookup 帶 Paging 應透傳；SearchText 空白不組過濾")]
        public void GetLookup_ThroughJsonRpc_PreservesExplicitPaging()
        {
            var table = new DataTable("Employee");
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));

            var stubRepository = new StubDataFormRepository(table);
            var executor = CreateExecutor(stubRepository);

            var request = new JsonRpcRequest
            {
                Method = $"Employee.{FormActions.GetLookup}",
                Params = new JsonRpcParams
                {
                    Value = new GetLookupRequest
                    {
                        Paging = new PagingOptions { Page = 3, PageSize = 20, IncludeTotalCount = true },
                    },
                },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            Assert.Null(stubRepository.LastFilter);
            Assert.NotNull(stubRepository.LastPaging);
            Assert.Equal(3, stubRepository.LastPaging!.Page);
            Assert.Equal(20, stubRepository.LastPaging.PageSize);
            Assert.True(stubRepository.LastPaging.IncludeTotalCount);
        }

        private JsonRpcExecutor CreateExecutor(IDataFormRepository stubRepository)
        {
            var stubFactory = new StubFormRepositoryFactory(stubRepository);
            var overrideServices = new TestOverrideServiceProvider(
                _fx.Provider,
                (typeof(IFormRepositoryFactory), stubFactory));

            var boFactory = new BusinessObjectFactory(
                overrideServices,
                _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<ISessionInfoService>(),
                _fx.GetRequiredService<ILanguageService>(),
                _fx.GetRequiredService<IFormBoTypeResolver>());

            return new JsonRpcExecutor(
                boFactory,
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = TestSessionFactory.CreateAccessToken(_fx),
                IsLocalCall = true,
            };
        }

        private sealed class StubFormRepositoryFactory : IFormRepositoryFactory
        {
            private readonly IDataFormRepository _data;
            public StubFormRepositoryFactory(IDataFormRepository data) { _data = data; }
            public IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken) => _data;
            public IReportFormRepository CreateReportFormRepository(string progId)
                => throw new NotSupportedException();
        }

        private sealed class StubDataFormRepository : IDataFormRepository
        {
            private readonly DataTable _table;

            public StubDataFormRepository(DataTable table) { _table = table; }

            public string? LastSelectFields { get; private set; }
            public FilterNode? LastFilter { get; private set; }
            public PagingOptions? LastPaging { get; private set; }

            public DataFormListResult GetList(
                string selectFields,
                FilterNode? filter,
                SortFieldCollection? sortFields,
                PagingOptions? paging = null)
            {
                LastSelectFields = selectFields;
                LastFilter = filter;
                LastPaging = paging;
                return new DataFormListResult { Table = _table };
            }

            public DataSet GetNewData() => throw new NotSupportedException();

            public DataSet? GetData(Guid rowId, FilterNode? scopeFilter = null) => throw new NotSupportedException();

            public (DataSet? Refreshed, Dictionary<string, int> AffectedRows) Save(DataSet dataSet)
                => throw new NotSupportedException();

            public int Delete(Guid rowId, FilterNode? scopeFilter = null) => throw new NotSupportedException();

            public bool ExistsInScope(Guid rowId, FilterNode? scopeFilter) => true;
        }
    }
}
