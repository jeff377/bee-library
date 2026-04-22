using System.ComponentModel;
using Bee.Api.Client.ApiServiceProvider;
using Bee.Api.Client.Connectors;
using Bee.Api.Core;
using Bee.Tests.Shared;

namespace Bee.Api.Client.UnitTests
{
    [Collection("Initialize")]
    public class SystemApiConnectorTests
    {
        /// <summary>
        /// 測試 SystemApiConnector 的 CreateSession 方法。
        /// </summary>
        [DbFact]
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

        [Fact]
        [DisplayName("SystemApiConnector Local 建構子應建立 LocalApiServiceProvider")]
        public void Constructor_Local_SetsAccessTokenAndLocalProvider()
        {
            var token = Guid.NewGuid();
            var connector = new SystemApiConnector(token);

            Assert.Equal(token, connector.AccessToken);
            Assert.IsType<LocalApiServiceProvider>(connector.Provider);
        }

        [Fact]
        [DisplayName("SystemApiConnector Remote 建構子應建立 RemoteApiServiceProvider")]
        public void Constructor_Remote_SetsAccessTokenAndRemoteProvider()
        {
            var token = Guid.NewGuid();
            var connector = new SystemApiConnector("http://example.com/api", token);

            Assert.Equal(token, connector.AccessToken);
            Assert.IsType<RemoteApiServiceProvider>(connector.Provider);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("SystemApiConnector Remote 建構子空白 endpoint 應拋 ArgumentException")]
        public void Constructor_RemoteEmptyEndpoint_ThrowsArgumentException(string? endpoint)
        {
            Assert.Throws<ArgumentException>(() => new SystemApiConnector(endpoint!, Guid.NewGuid()));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [DisplayName("SystemApiConnector.ExecuteAsync 空白 action 應拋 ArgumentException")]
        public async Task ExecuteAsync_EmptyAction_ThrowsArgumentException(string? action)
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await connector.ExecuteAsync<object>(action!, new object(), PayloadFormat.Plain));
        }

        [DbFact]
        [DisplayName("SystemApiConnector.PingAsync 本機連線應成功回應")]
        public async Task PingAsync_LocalConnector_Succeeds()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var exception = await Record.ExceptionAsync(() => connector.PingAsync());
            Assert.Null(exception);
        }

        [DbFact]
        [DisplayName("SystemApiConnector.Ping 同步本機連線應成功回應")]
        public void Ping_LocalConnector_Succeeds()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var exception = Record.Exception(() => connector.Ping());
            Assert.Null(exception);
        }

    }
}
