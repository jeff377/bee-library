using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Tests.Shared;

namespace Bee.Api.Client.UnitTests
{
    [Collection("Initialize")]
    public class ApiConnectValidatorTests
    {
        [LocalOnlyTheory]
        [DisplayName("ApiConnectValidator 驗證 URL 應回傳遠端連線類型")]
        [InlineData("http://localhost/jsonrpc/api")]
        //[InlineData("http://localhost/jsonrpc_aspnet/api")]
        public void Validate_ValidUrl_ReturnsRemoteConnectType(string apiUrl)
        {
            var validator = new ApiConnectValidator();
            var connectType = validator.Validate(apiUrl);

            Assert.Equal(ConnectType.Remote, connectType);  // 確認連線方式為遠端連線
        }
    }
}
