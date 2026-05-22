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
    /// <c>Employee.Save</c> 的 <c>SaveRequest.DataSet</c> 經 ApiInputConverter
    /// 對拷到 <c>SaveArgs.DataSet</c> 時 row state 保留,且 stub 回傳的
    /// refreshed DataSet 與 AffectedRows 經 ApiOutputConverter 對拷回 wire
    /// response。
    /// </summary>
    public class SaveJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public SaveJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("Employee.Save 經 JsonRpcExecutor 應保留 RowState 並回傳 refreshed DataSet + AffectedRows")]
        public void Save_ThroughJsonRpc_PreservesRowStatesAndReturnsRefreshed()
        {
            // 準備一份 DataSet,master 含 1 個 Added row
            var input = new DataSet("Employee");
            var master = new DataTable("Employee");
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add(SysFields.Name, typeof(string));
            var rowId = Guid.NewGuid();
            master.Rows.Add(rowId, "全新員工");
            input.Tables.Add(master);

            // Refreshed DataSet 含 server-generated 欄位(模擬 trigger 寫回)
            var refreshed = new DataSet("Employee");
            var refreshedMaster = new DataTable("Employee");
            refreshedMaster.Columns.Add(SysFields.RowId, typeof(Guid));
            refreshedMaster.Columns.Add(SysFields.Name, typeof(string));
            refreshedMaster.Columns.Add(SysFields.InsertTime, typeof(DateTime));
            refreshedMaster.Rows.Add(rowId, "全新員工", new DateTime(2026, 5, 23, 0, 0, 0, DateTimeKind.Utc));
            refreshed.Tables.Add(refreshedMaster);
            refreshed.AcceptChanges();

            var stub = new StubCrudDataFormRepository
            {
                SaveResult = (refreshed, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Employee"] = 1,
                }),
            };
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
                Method = $"Employee.{FormActions.Save}",
                Params = new JsonRpcParams { Value = new SaveRequest { DataSet = input } },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<SaveResponse>(response.Result!.Value);
            // 框架不變式:refreshed DataSet 也以 ProgId 為 DataSetName。
            Assert.NotNull(result.DataSet);
            Assert.Equal("Employee", result.DataSet!.DataSetName);
            Assert.Equal(1, result.AffectedRows["Employee"]);

            // BO 收到的 DataSet 應保留 Added row state
            Assert.NotNull(stub.LastSavedDataSet);
            var savedMaster = stub.LastSavedDataSet!.Tables["Employee"]!;
            Assert.Equal(DataRowState.Added, savedMaster.Rows[0].RowState);
        }
    }
}
