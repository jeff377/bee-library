using System.ComponentModel;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// <see cref="ApiKeyStorage"/> 的單元測試，與 <see cref="EndpointStorageTests"/> 對稱：
    /// 預設實作以 <see cref="ClientInfo.ClientSettings"/> 為後盾。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ApiKeyStorageTests
    {
        [Fact]
        [DisplayName("LoadApiKey 應從 ClientInfo.ClientSettings.ApiKey 讀取金鑰")]
        public void LoadApiKey_ReturnsClientSettingsApiKey()
        {
            var storage = new ApiKeyStorage();
            var original = ClientInfo.ClientSettings.ApiKey;
            try
            {
                ClientInfo.ClientSettings.ApiKey = "read-test.secret";
                Assert.Equal("read-test.secret", storage.LoadApiKey());
            }
            finally
            {
                ClientInfo.ClientSettings.ApiKey = original;
            }
        }

        [Fact]
        [DisplayName("SetApiKey 應更新 ClientInfo.ClientSettings.ApiKey 的值")]
        public void SetApiKey_ValidValue_UpdatesClientSettingsApiKey()
        {
            var storage = new ApiKeyStorage();
            var original = ClientInfo.ClientSettings.ApiKey;
            try
            {
                storage.SetApiKey("set-test.secret");
                Assert.Equal("set-test.secret", ClientInfo.ClientSettings.ApiKey);
            }
            finally
            {
                ClientInfo.ClientSettings.ApiKey = original;
            }
        }

        [Fact]
        [DisplayName("SaveApiKey 應更新 ClientInfo.ClientSettings.ApiKey 並儲存設定")]
        public void SaveApiKey_ValidValue_UpdatesApiKeyAndSaves()
        {
            var storage = new ApiKeyStorage();
            var original = ClientInfo.ClientSettings.ApiKey;
            try
            {
                storage.SaveApiKey("save-test.secret");
                Assert.Equal("save-test.secret", ClientInfo.ClientSettings.ApiKey);
            }
            finally
            {
                ClientInfo.ClientSettings.ApiKey = original;
            }
        }
    }
}
