using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.Form;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests.Form
{
    /// <summary>
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip:確認
    /// <c>Employee.GetData</c> 的 <c>RowId</c> 經 ApiInputConverter 對拷到
    /// <c>GetDataArgs</c>,且 stub 回傳的 DataSet 經 ApiOutputConverter 對拷
    /// 回 wire response,並保留 <c>DataSetName == ProgId</c> 的框架不變式。
    /// </summary>
    public class GetDataJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public GetDataJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("Employee.GetData 經 JsonRpcExecutor 應透傳 RowId 並回傳完整 DataSet")]
        public void GetData_ThroughJsonRpc_PreservesRowIdAndReturnsDataSet()
        {
            var rowId = Guid.NewGuid();
            var dataSet = new DataSet("Employee");
            var master = new DataTable("Employee");
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add(SysFields.Name, typeof(string));
            master.Rows.Add(rowId, "員工甲");
            dataSet.Tables.Add(master);
            dataSet.AcceptChanges();

            var stub = new StubCrudDataFormRepository { GetDataResult = dataSet };
            var stubFactory = new StubCrudFormRepositoryFactory(stub);

            var overrideServices = new TestOverrideServiceProvider(
                _fx.Provider,
                (typeof(IFormRepositoryFactory), stubFactory));

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

            var request = new JsonRpcRequest
            {
                Method = $"Employee.{FormActions.GetData}",
                Params = new JsonRpcParams { Value = new GetDataRequest { RowId = rowId } },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<GetDataResponse>(response.Result!.Value);
            Assert.NotNull(result.DataSet);
            // 框架不變式:DataSet.DataSetName == ProgId,Tables[ProgId] 即 Master。
            Assert.Equal("Employee", result.DataSet!.DataSetName);
            Assert.Equal("員工甲", result.DataSet.Tables["Employee"]!.Rows[0][SysFields.Name]);

            Assert.Equal(rowId, stub.LastRowId);
        }
    }
}
