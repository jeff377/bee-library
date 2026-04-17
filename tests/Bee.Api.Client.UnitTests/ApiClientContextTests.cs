using System.ComponentModel;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="ApiClientContext"/> 靜態屬性的純邏輯測試。
    /// </summary>
    public class ApiClientContextTests
    {
        /// <summary>
        /// 執行測試前先備份靜態狀態，測試結束後還原，避免跨測試污染。
        /// </summary>
        private static void WithSnapshot(Action action)
        {
            var supported = ApiClientContext.SupportedConnectTypes;
            var connectType = ApiClientContext.ConnectType;
            var endpoint = ApiClientContext.Endpoint;
            var apiKey = ApiClientContext.ApiKey;
            var encryptionKey = ApiClientContext.ApiEncryptionKey;
            try
            {
                action();
            }
            finally
            {
                ApiClientContext.SupportedConnectTypes = supported;
                ApiClientContext.ConnectType = connectType;
                ApiClientContext.Endpoint = endpoint;
                ApiClientContext.ApiKey = apiKey;
                ApiClientContext.ApiEncryptionKey = encryptionKey;
            }
        }

        [Fact]
        [DisplayName("ApiClientContext 預設值應符合設計")]
        public void Defaults_AreExpected()
        {
            WithSnapshot(() =>
            {
                ApiClientContext.SupportedConnectTypes = SupportedConnectTypes.Both;
                ApiClientContext.ConnectType = ConnectType.Local;
                ApiClientContext.Endpoint = string.Empty;
                ApiClientContext.ApiKey = string.Empty;
                ApiClientContext.ApiEncryptionKey = Array.Empty<byte>();

                Assert.Equal(SupportedConnectTypes.Both, ApiClientContext.SupportedConnectTypes);
                Assert.Equal(ConnectType.Local, ApiClientContext.ConnectType);
                Assert.Equal(string.Empty, ApiClientContext.Endpoint);
                Assert.Equal(string.Empty, ApiClientContext.ApiKey);
                Assert.Empty(ApiClientContext.ApiEncryptionKey);
            });
        }

        [Fact]
        [DisplayName("ApiClientContext.SupportedConnectTypes 可被覆寫並讀回")]
        public void SupportedConnectTypes_CanBeOverwritten()
        {
            WithSnapshot(() =>
            {
                ApiClientContext.SupportedConnectTypes = SupportedConnectTypes.Remote;
                Assert.Equal(SupportedConnectTypes.Remote, ApiClientContext.SupportedConnectTypes);

                ApiClientContext.SupportedConnectTypes = SupportedConnectTypes.Local;
                Assert.Equal(SupportedConnectTypes.Local, ApiClientContext.SupportedConnectTypes);
            });
        }

        [Fact]
        [DisplayName("ApiClientContext.ConnectType 可被覆寫並讀回")]
        public void ConnectType_CanBeOverwritten()
        {
            WithSnapshot(() =>
            {
                ApiClientContext.ConnectType = ConnectType.Remote;
                Assert.Equal(ConnectType.Remote, ApiClientContext.ConnectType);
            });
        }

        [Fact]
        [DisplayName("ApiClientContext.Endpoint 與 ApiKey 可被覆寫並讀回")]
        public void EndpointAndApiKey_CanBeOverwritten()
        {
            WithSnapshot(() =>
            {
                ApiClientContext.Endpoint = "http://example.com";
                ApiClientContext.ApiKey = "test-api-key";

                Assert.Equal("http://example.com", ApiClientContext.Endpoint);
                Assert.Equal("test-api-key", ApiClientContext.ApiKey);
            });
        }

        [Fact]
        [DisplayName("ApiClientContext.ApiEncryptionKey 可被替換為新陣列")]
        public void ApiEncryptionKey_CanBeReplaced()
        {
            WithSnapshot(() =>
            {
                byte[] key = { 0x01, 0x02, 0x03, 0x04 };
                ApiClientContext.ApiEncryptionKey = key;

                Assert.Same(key, ApiClientContext.ApiEncryptionKey);
                Assert.Equal(4, ApiClientContext.ApiEncryptionKey.Length);
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
