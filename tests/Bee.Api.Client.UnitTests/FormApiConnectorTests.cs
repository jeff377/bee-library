using System.ComponentModel;
using Bee.Api.Client.Providers;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="FormApiConnector"/> 建構子與參數驗證的純邏輯測試。
    /// </summary>
    public class FormApiConnectorTests
    {
        private const string TestProgId = "Employee";

        [Fact]
        [DisplayName("FormApiConnector Local 建構子應設定 ProgId 與 LocalApiProvider")]
        public void Constructor_Local_SetsProgIdAndProvider()
        {
            var token = Guid.NewGuid();
            var connector = new FormApiConnector(token, TestProgId);

            Assert.Equal(token, connector.AccessToken);
            Assert.Equal(TestProgId, connector.ProgId);
            Assert.IsType<LocalApiProvider>(connector.Provider);
        }

        [Fact]
        [DisplayName("FormApiConnector Remote 建構子應設定 ProgId 與 RemoteApiProvider")]
        public void Constructor_Remote_SetsProgIdAndProvider()
        {
            var token = Guid.NewGuid();
            var connector = new FormApiConnector("http://example.com/api", token, TestProgId);

            Assert.Equal(token, connector.AccessToken);
            Assert.Equal(TestProgId, connector.ProgId);
            Assert.IsType<RemoteApiProvider>(connector.Provider);
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

        [Fact]
        [DisplayName("FormApiConnector.SaveAsync 傳入 null DataSet 應拋 ArgumentNullException")]
        public async Task SaveAsync_NullDataSet_ThrowsArgumentNullException()
        {
            var connector = new FormApiConnector(Guid.NewGuid(), TestProgId);
            await Assert.ThrowsAsync<ArgumentNullException>(() => connector.SaveAsync(null!));
        }

        [Fact]
        [DisplayName("FormApiConnector CRUD async 方法不應有同名同步版本(async-only 慣例)")]
        public void CrudAsyncMethods_HaveNoSyncCounterpart()
        {
            var type = typeof(FormApiConnector);

            // 計畫 §1.3 確立的新慣例:CRUD 4 個 action 僅以 async 方法暴露,
            // 不再順手加同步 wrapper(避免 sync-over-async 在 Blazor Server
            // 切半 thread pool 的反 pattern)。
            string[] crudAsyncNames = { "GetNewDataAsync", "GetDataAsync", "SaveAsync", "DeleteAsync" };
            foreach (var asyncName in crudAsyncNames)
            {
                Assert.NotNull(type.GetMethod(asyncName));

                var syncName = asyncName.Replace("Async", string.Empty, StringComparison.Ordinal);
                Assert.Null(type.GetMethod(syncName));
            }
        }
    }
}
