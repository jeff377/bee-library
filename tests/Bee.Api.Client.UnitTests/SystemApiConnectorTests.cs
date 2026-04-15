using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Tests.Shared;

namespace Bee.Api.Client.UnitTests
{
    [Collection("Initialize")]
    public class SystemApiConnectorTests
    {
        /// <summary>
        /// 測試 SystemApiConnector 的 CreateSession 方法。
        /// </summary>
        [LocalOnlyFact]
        [DisplayName("SystemApiConnector CreateSession 應回傳有效的 AccessToken")]
        public void CreateSession_ValidArgs_ReturnsValidToken()
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
