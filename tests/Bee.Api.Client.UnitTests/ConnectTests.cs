using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Tests.Shared;

namespace Bee.Api.Client.UnitTests
{
    [Collection("Initialize")]
    public class ConnectTests
    {
        static ConnectTests()
        {
        }

        [LocalOnlyTheory]
        [DisplayName("ApiConnectValidator 驗證 URL 應回傳遠端連線類型")]
        [InlineData("http://localhost/jsonrpc/api")]
        //[InlineData("http://localhost/jsonrpc_aspnet/api")]
        public void ApiConnectValidator_ValidUrl_ReturnsRemoteConnectType(string apiUrl)
        {
            var validator = new ApiConnectValidator();
            var connectType = validator.Validate(apiUrl);

            Assert.Equal(ConnectType.Remote, connectType);  // 確認連線方式為遠端連線
        }

        /// <summary>
        /// 測試 SystemApiConnector 的 CreateSession 方法。
        /// </summary>
        [LocalOnlyFact]
        [DisplayName("SystemApiConnector CreateSession 應回傳有效的 AccessToken")]
        public void SystemApiConnector_CreateSession_ReturnsValidToken()
        {
            // Arrange
            string userId = "001";
            int expiresIn = 600;
            bool oneTime = false;

            // 產生一個隨機 Guid 作為 accessToken（僅用於初始化，CreateSession 會回傳新的 token）
            Guid accessToken = Guid.NewGuid();
            var connector = new SystemApiConnector(accessToken);

            // Act
            Guid newToken = connector.CreateSession(userId, expiresIn, oneTime);

            // Assert
            Assert.NotEqual(Guid.Empty, newToken); // 應取得有效 accessToken
        }
    }
}
