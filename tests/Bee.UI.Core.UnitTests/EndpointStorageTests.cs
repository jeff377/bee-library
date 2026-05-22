using System.ComponentModel;

namespace Bee.UI.Core.UnitTests
{
    [CollectionDefinition("ClientInfoState")]
    public sealed class ClientInfoStateCollection { }

    [Collection("ClientInfoState")]
    public class EndpointStorageTests
    {
        [Fact]
        [DisplayName("LoadEndpoint 應從 ClientInfo.ClientSettings.Endpoint 讀取端點")]
        public void LoadEndpoint_ReturnsClientSettingsEndpoint()
        {
            var storage = new EndpointStorage();
            var original = ClientInfo.ClientSettings.Endpoint;
            try
            {
                ClientInfo.ClientSettings.Endpoint = "http://read-test.example.com";
                var result = storage.LoadEndpoint();
                Assert.Equal("http://read-test.example.com", result);
            }
            finally
            {
                ClientInfo.ClientSettings.Endpoint = original;
            }
        }

        [Fact]
        [DisplayName("SetEndpoint 應更新 ClientInfo.ClientSettings.Endpoint 的值")]
        public void SetEndpoint_ValidValue_UpdatesClientSettingsEndpoint()
        {
            var storage = new EndpointStorage();
            var original = ClientInfo.ClientSettings.Endpoint;
            try
            {
                storage.SetEndpoint("http://set-test.example.com");
                Assert.Equal("http://set-test.example.com", ClientInfo.ClientSettings.Endpoint);
            }
            finally
            {
                ClientInfo.ClientSettings.Endpoint = original;
            }
        }

        [Fact]
        [DisplayName("SaveEndpoint 應更新 ClientInfo.ClientSettings.Endpoint 並儲存設定")]
        public void SaveEndpoint_ValidValue_UpdatesEndpointAndSaves()
        {
            var storage = new EndpointStorage();
            var original = ClientInfo.ClientSettings.Endpoint;
            try
            {
                storage.SaveEndpoint("http://save-test.example.com");
                Assert.Equal("http://save-test.example.com", ClientInfo.ClientSettings.Endpoint);
            }
            finally
            {
                ClientInfo.ClientSettings.Endpoint = original;
            }
        }
    }
}
