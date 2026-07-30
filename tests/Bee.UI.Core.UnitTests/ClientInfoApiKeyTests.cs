using System.ComponentModel;
using Bee.Api.Client;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// <see cref="ClientInfo"/> 的 API 金鑰接縫：讀寫走可替換的 <see cref="IApiKeyStorage"/>，
    /// 以及 <see cref="ClientInfo.ApplyApiKey"/>「應用內建值只當首次啟動的種子」的語意——
    /// 這正是「更換金鑰不需重新編譯用戶端」成立的關鍵。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoApiKeyTests
    {
        private sealed class FakeApiKeyStorage : IApiKeyStorage
        {
            public string Stored { get; set; } = string.Empty;
            public int SaveCount { get; private set; }

            public string LoadApiKey() => Stored;

            public void SetApiKey(string apiKey) => Stored = apiKey;

            public void SaveApiKey(string apiKey)
            {
                Stored = apiKey;
                SaveCount++;
            }
        }

        /// <summary>
        /// 以替換的 storage 執行測試，結束後還原兩個 process-wide static。
        /// </summary>
        private static void WithFakeStorage(Action<FakeApiKeyStorage> action)
        {
            var originalStorage = ClientInfo.ApiKeyStorage;
            var originalKey = ApiClientInfo.ApiKey;
            var fake = new FakeApiKeyStorage();
            ClientInfo.ApiKeyStorage = fake;
            try
            {
                action(fake);
            }
            finally
            {
                ClientInfo.ApiKeyStorage = originalStorage;
                ApiClientInfo.ApiKey = originalKey;
            }
        }

        [Fact]
        [DisplayName("GetApiKey 應轉交給 ApiKeyStorage")]
        public void GetApiKey_DelegatesToStorage()
        {
            WithFakeStorage(fake =>
            {
                fake.Stored = "app-id.secret";

                Assert.Equal("app-id.secret", ClientInfo.GetApiKey());
            });
        }

        [Fact]
        [DisplayName("SetApiKey 應持久化並立即套用到後續 API 呼叫")]
        public void SetApiKey_PersistsAndApplies()
        {
            WithFakeStorage(fake =>
            {
                ClientInfo.SetApiKey("new-app.secret");

                Assert.Equal("new-app.secret", fake.Stored);
                Assert.Equal(1, fake.SaveCount);
                Assert.Equal("new-app.secret", ApiClientInfo.ApiKey);
            });
        }

        [Fact]
        [DisplayName("ApplyApiKey 於存放為空時應以應用內建值作為種子並寫入")]
        public void ApplyApiKey_EmptyStorage_SeedsWithDefault()
        {
            WithFakeStorage(fake =>
            {
                ClientInfo.ApplyApiKey("shipped-default");

                Assert.Equal("shipped-default", fake.Stored);
                Assert.Equal(1, fake.SaveCount);
                Assert.Equal("shipped-default", ApiClientInfo.ApiKey);
            });
        }

        [Fact]
        [DisplayName("ApplyApiKey 於已有存放值時應採用存放值，內建值不覆蓋")]
        public void ApplyApiKey_ExistingStorage_KeepsStoredValue()
        {
            WithFakeStorage(fake =>
            {
                fake.Stored = "configured.secret";

                ClientInfo.ApplyApiKey("shipped-default");

                Assert.Equal("configured.secret", fake.Stored);
                // 已有值就不該再寫一次；這也是「更換後不會被下次啟動蓋回去」的保證。
                Assert.Equal(0, fake.SaveCount);
                Assert.Equal("configured.secret", ApiClientInfo.ApiKey);
            });
        }

        [Fact]
        [DisplayName("ApplyApiKey 於未給內建值且存放為空時應套用空字串且不寫入")]
        public void ApplyApiKey_NoDefaultAndEmptyStorage_AppliesEmpty()
        {
            WithFakeStorage(fake =>
            {
                ApiClientInfo.ApiKey = "stale";

                ClientInfo.ApplyApiKey();

                Assert.Equal(string.Empty, ApiClientInfo.ApiKey);
                Assert.Equal(0, fake.SaveCount);
            });
        }
    }
}
