using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.Form;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests.Form
{
    /// <summary>
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip:確認
    /// <c>Employee.Delete</c> 的 <c>DeleteRequest.RowId</c> 經 ApiInputConverter
    /// 對拷到 <c>DeleteArgs.RowId</c>,stub 回傳的 <c>RowsAffected</c> 經
    /// ApiOutputConverter 對拷回 wire response。
    /// </summary>
    public class DeleteJsonRpcRoundTripTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public DeleteJsonRpcRoundTripTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("Employee.Delete 經 JsonRpcExecutor 應透傳 RowId 並回傳 RowsAffected")]
        public void Delete_ThroughJsonRpc_PreservesRowIdAndReturnsRowsAffected()
        {
            var rowId = Guid.NewGuid();
            var stub = new StubCrudDataFormRepository { DeleteResult = 1 };
            var stubFactory = new StubCrudFormRepositoryFactory(stub);

            var overrideServices = new TestOverrideServiceProvider(
                _fx.Provider,
                (typeof(IFormRepositoryFactory), stubFactory));

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
                Method = $"Employee.{FormActions.Delete}",
                Params = new JsonRpcParams { Value = new DeleteRequest { RowId = rowId } },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<DeleteResponse>(response.Result!.Value);
            Assert.Equal(1, result.RowsAffected);
            Assert.Equal(rowId, stub.LastRowId);
        }
    }
}
