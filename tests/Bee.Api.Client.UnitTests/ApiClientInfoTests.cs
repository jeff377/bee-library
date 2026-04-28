using System.ComponentModel;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="ApiClientInfo"/> 靜態屬性的純邏輯測試。
    /// </summary>
    public class ApiClientInfoTests
    {
        /// <summary>
        /// 執行測試前先備份靜態狀態，測試結束後還原，避免跨測試污染。
        /// </summary>
        private static void WithSnapshot(Action action)
        {
            var supported = ApiClientInfo.SupportedConnectTypes;
            var connectType = ApiClientInfo.ConnectType;
            var endpoint = ApiClientInfo.Endpoint;
            var apiKey = ApiClientInfo.ApiKey;
            var encryptionKey = ApiClientInfo.ApiEncryptionKey;
            try
            {
                action();
            }
            finally
            {
                ApiClientInfo.SupportedConnectTypes = supported;
                ApiClientInfo.ConnectType = connectType;
                ApiClientInfo.Endpoint = endpoint;
                ApiClientInfo.ApiKey = apiKey;
                ApiClientInfo.ApiEncryptionKey = encryptionKey;
            }
        }

        [Fact]
        [DisplayName("ApiClientInfo 預設值應符合設計")]
        public void Defaults_AreExpected()
        {
            WithSnapshot(() =>
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                ApiClientInfo.ConnectType = ConnectType.Local;
                ApiClientInfo.Endpoint = string.Empty;
                ApiClientInfo.ApiKey = string.Empty;
                ApiClientInfo.ApiEncryptionKey = Array.Empty<byte>();

                Assert.Equal(SupportedConnectTypes.Both, ApiClientInfo.SupportedConnectTypes);
                Assert.Equal(ConnectType.Local, ApiClientInfo.ConnectType);
                Assert.Equal(string.Empty, ApiClientInfo.Endpoint);
                Assert.Equal(string.Empty, ApiClientInfo.ApiKey);
                Assert.Empty(ApiClientInfo.ApiEncryptionKey);
            });
        }

        [Fact]
        [DisplayName("ApiClientInfo.SupportedConnectTypes 可被覆寫並讀回")]
        public void SupportedConnectTypes_CanBeOverwritten()
        {
            WithSnapshot(() =>
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
                Assert.Equal(SupportedConnectTypes.Remote, ApiClientInfo.SupportedConnectTypes);

                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Local;
                Assert.Equal(SupportedConnectTypes.Local, ApiClientInfo.SupportedConnectTypes);
            });
        }

        [Fact]
        [DisplayName("ApiClientInfo.ConnectType 可被覆寫並讀回")]
        public void ConnectType_CanBeOverwritten()
        {
            WithSnapshot(() =>
            {
                ApiClientInfo.ConnectType = ConnectType.Remote;
                Assert.Equal(ConnectType.Remote, ApiClientInfo.ConnectType);
            });
        }

        [Fact]
        [DisplayName("ApiClientInfo.Endpoint 與 ApiKey 可被覆寫並讀回")]
        public void EndpointAndApiKey_CanBeOverwritten()
        {
            WithSnapshot(() =>
            {
                ApiClientInfo.Endpoint = "http://example.com";
                ApiClientInfo.ApiKey = "test-api-key";

                Assert.Equal("http://example.com", ApiClientInfo.Endpoint);
                Assert.Equal("test-api-key", ApiClientInfo.ApiKey);
            });
        }

        [Fact]
        [DisplayName("ApiClientInfo.ApiEncryptionKey 可被替換為新陣列")]
        public void ApiEncryptionKey_CanBeReplaced()
        {
            WithSnapshot(() =>
            {
                byte[] key = { 0x01, 0x02, 0x03, 0x04 };
                ApiClientInfo.ApiEncryptionKey = key;

                Assert.Same(key, ApiClientInfo.ApiEncryptionKey);
                Assert.Equal(4, ApiClientInfo.ApiEncryptionKey.Length);
            });
        }

        [Fact]
        [DisplayName("SupportedConnectTypes.Both 等於 Local 與 Remote 的 OR")]
        public void SupportedConnectTypes_Both_EqualsLocalOrRemote()
        {
            Assert.Equal(SupportedConnectTypes.Local | SupportedConnectTypes.Remote, SupportedConnectTypes.Both);
            Assert.True(SupportedConnectTypes.Both.HasFlag(SupportedConnectTypes.Local));
            Assert.True(SupportedConnectTypes.Both.HasFlag(SupportedConnectTypes.Remote));
        }
    }
}
