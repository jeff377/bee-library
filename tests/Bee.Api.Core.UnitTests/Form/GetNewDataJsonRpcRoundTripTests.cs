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
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip:將
    /// <c>Employee.GetNewData</c> 透過 executor 派發到 BO,並由 stub repository
    /// 回傳已知 skeleton DataSet,驗證命名 convention 對拷 + DataSet 還原。
    /// </summary>
    public class GetNewDataJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public GetNewDataJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("Employee.GetNewData 經 JsonRpcExecutor 應派發到 BO 並回傳 stub skeleton DataSet")]
        public void GetNewData_ThroughJsonRpc_DispatchesAndReturnsDataSet()
        {
            var skeleton = new DataSet("Employee");
            var master = new DataTable("Employee");
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add(SysFields.Name, typeof(string));
            var skeletonRowId = Guid.NewGuid();
            master.Rows.Add(skeletonRowId, "預設員工");
            skeleton.Tables.Add(master);

            var stub = new StubCrudDataFormRepository { GetNewDataResult = skeleton };
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
                Method = $"Employee.{FormActions.GetNewData}",
                Params = new JsonRpcParams { Value = new GetNewDataRequest() },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<GetNewDataResponse>(response.Result!.Value);
            Assert.NotNull(result.DataSet);
            // 框架不變式:DataSet.DataSetName == ProgId,Tables[ProgId] 即 Master。
            Assert.Equal("Employee", result.DataSet!.DataSetName);
            Assert.Equal(skeletonRowId, (Guid)result.DataSet.Tables["Employee"]!.Rows[0][SysFields.RowId]);

            Assert.True(stub.GetNewDataCalled);
        }
    }
}
