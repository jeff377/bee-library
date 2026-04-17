using System.ComponentModel;
using Bee.Api.Client.ApiServiceProvider;
using Bee.Api.Client.Connectors;
using Bee.Api.Core;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="FormApiConnector"/> 建構子與參數驗證的純邏輯測試。
    /// </summary>
    public class FormApiConnectorTests
    {
        private const string TestProgId = "Employee";

        [Fact]
        [DisplayName("FormApiConnector Local 建構子應設定 ProgId 與 LocalApiServiceProvider")]
        public void Constructor_Local_SetsProgIdAndProvider()
        {
            var token = Guid.NewGuid();
            var connector = new FormApiConnector(token, TestProgId);

            Assert.Equal(token, connector.AccessToken);
            Assert.Equal(TestProgId, connector.ProgId);
            Assert.IsType<LocalApiServiceProvider>(connector.Provider);
        }

        [Fact]
        [DisplayName("FormApiConnector Remote 建構子應設定 ProgId 與 RemoteApiServiceProvider")]
        public void Constructor_Remote_SetsProgIdAndProvider()
        {
            var token = Guid.NewGuid();
            var connector = new FormApiConnector("http://example.com/api", token, TestProgId);

            Assert.Equal(token, connector.AccessToken);
            Assert.Equal(TestProgId, connector.ProgId);
            Assert.IsType<RemoteApiServiceProvider>(connector.Provider);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("FormApiConnector Remote 建構子空白 endpoint 應拋 ArgumentException")]
        public void Constructor_RemoteEmptyEndpoint_ThrowsArgumentException(string? endpoint)
        {
            Assert.Throws<ArgumentException>(() => new FormApiConnector(endpoint!, Guid.NewGuid(), TestProgId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [DisplayName("FormApiConnector.ExecuteAsync 空白 action 應拋 ArgumentException")]
        public async Task ExecuteAsync_EmptyAction_ThrowsArgumentException(string? action)
        {
            var connector = new FormApiConnector(Guid.NewGuid(), TestProgId);
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await connector.ExecuteAsync<object>(action!, new object(), PayloadFormat.Plain));
        }
    }
}
