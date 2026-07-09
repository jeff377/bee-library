using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Definition;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="ClientDefineAccess"/> 的 async 型別化存取、快取與例外傳遞測試。
    /// 以 local <see cref="SystemApiConnector"/> 建構，需要 <c>ApiClientInfo.LocalServiceProvider</c>
    /// 由 <see cref="Bee.Tests.Shared.GlobalFixture"/> 一次性 wire up；
    /// 透過 <see cref="Bee.Tests.Shared.BeeTestFixture"/> 的 ctor 觸發完成。
    /// </summary>
    public class ClientDefineAccessTests : IClassFixture<Bee.Tests.Shared.BeeTestFixture>
    {
        public ClientDefineAccessTests(Bee.Tests.Shared.BeeTestFixture _)
        {
            // fixture 僅用於觸發 GlobalFixture 一次性 wire up ApiClientInfo.LocalServiceProvider。
        }

        private static ClientDefineAccess CreateAccess()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            return new ClientDefineAccess(connector);
        }

        [Fact]
        [DisplayName("ClientDefineAccess 建構子以 SystemApiConnector 建立時不應拋例外")]
        public void Constructor_WithConnector_DoesNotThrow()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new ClientDefineAccess(connector);
            Assert.NotNull(access);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetSystemSettingsAsync 本機連線應回傳系統設定")]
        public async Task GetSystemSettingsAsync_LocalConnector_ReturnsSettings()
        {
            var access = CreateAccess();

            var settings = await access.GetSystemSettingsAsync();

            Assert.NotNull(settings);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetDatabaseSettingsAsync 本機連線應回傳資料庫設定")]
        public async Task GetDatabaseSettingsAsync_LocalConnector_ReturnsSettings()
        {
            var access = CreateAccess();

            var settings = await access.GetDatabaseSettingsAsync();

            Assert.NotNull(settings);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetDbCategorySettingsAsync 本機連線應回傳資料庫類別設定")]
        public async Task GetDbCategorySettingsAsync_LocalConnector_ReturnsSettings()
        {
            var access = CreateAccess();

            var settings = await access.GetDbCategorySettingsAsync();

            Assert.NotNull(settings);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetFormSchemaAsync 本機連線應回傳表單結構定義")]
        public async Task GetFormSchemaAsync_LocalConnector_ReturnsFormSchema()
        {
            var access = CreateAccess();

            var schema = await access.GetFormSchemaAsync("Employee");

            Assert.NotNull(schema);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetCurrencySettingsAsync 本機連線應可取得（未部署則回 null，不拋例外）")]
        public async Task GetCurrencySettingsAsync_LocalConnector_DoesNotThrow()
        {
            var access = CreateAccess();

            // 幣別 master 未必部署於測試 Define fixture；無論回傳值或 null，取用路徑皆不應拋例外。
            var exception = await Record.ExceptionAsync(() => access.GetCurrencySettingsAsync());

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetUnitSettingsAsync 本機連線應可取得（未部署則回 null，不拋例外）")]
        public async Task GetUnitSettingsAsync_LocalConnector_DoesNotThrow()
        {
            var access = CreateAccess();

            var exception = await Record.ExceptionAsync(() => access.GetUnitSettingsAsync());

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetTableSchemaAsync 本機連線應回傳資料表結構定義")]
        public async Task GetTableSchemaAsync_LocalConnector_ReturnsTableSchema()
        {
            var access = CreateAccess();

            var schema = await access.GetTableSchemaAsync("common", "st_user");

            Assert.NotNull(schema);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetProgramSettingsAsync 應回傳程式設定")]
        public async Task GetProgramSettingsAsync_ReturnsProgramSettings()
        {
            var access = new ClientDefineAccess(new CountingConnector());

            var result = await access.GetProgramSettingsAsync();

            Assert.NotNull(result);
            Assert.IsType<ProgramSettings>(result);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetPermissionModelsAsync 應回傳權限模型")]
        public async Task GetPermissionModelsAsync_ReturnsPermissionModels()
        {
            var access = new ClientDefineAccess(new CountingConnector());

            var result = await access.GetPermissionModelsAsync();

            Assert.NotNull(result);
            Assert.IsType<PermissionModels>(result);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetFormLayoutAsync 應回傳表單排版")]
        public async Task GetFormLayoutAsync_ReturnsFormLayout()
        {
            var access = new ClientDefineAccess(new CountingConnector());

            var result = await access.GetFormLayoutAsync("Employee");

            Assert.NotNull(result);
            Assert.IsType<FormLayout>(result);
        }

        [Fact]
        [DisplayName("ClientDefineAccess.GetLanguageAsync 應回傳語言資源")]
        public async Task GetLanguageAsync_ReturnsLanguageResource()
        {
            var access = new ClientDefineAccess(new CountingConnector());

            var result = await access.GetLanguageAsync("en", "core");

            Assert.NotNull(result);
            Assert.IsType<LanguageResource>(result);
        }

        [Fact]
        [DisplayName("ClientDefineAccess 重複讀取同一鍵應使用快取回傳相同物件")]
        public async Task GetSystemSettingsAsync_SecondCall_ReturnsCachedObject()
        {
            var access = CreateAccess();

            var result1 = await access.GetSystemSettingsAsync();
            var result2 = await access.GetSystemSettingsAsync();

            Assert.NotNull(result1);
            Assert.Same(result1, result2);
        }

        [Fact]
        [DisplayName("同一鍵的並發 miss 應去重為單一 connector 抓取")]
        public async Task GetFormLayoutAsync_ConcurrentMiss_DeduplicatesToSingleFetch()
        {
            var connector = new GatedConnector();
            var access = new ClientDefineAccess(connector);

            // 兩個並發 miss：第一個同步插入 in-flight task，第二個命中同一 task。
            var t1 = access.GetFormLayoutAsync("L1");
            var t2 = access.GetFormLayoutAsync("L1");
            connector.Release();
            await Task.WhenAll(t1, t2);

            Assert.Equal(1, connector.GetDefineCallCount);
        }

        [Fact]
        [DisplayName("ClearCache 後同一鍵應重新向 connector 抓取（切換租戶不回舊疊加結果）")]
        public async Task ClearCache_AfterClear_RefetchesFromConnector()
        {
            var connector = new CountingConnector();
            var access = new ClientDefineAccess(connector);

            await access.GetFormLayoutAsync("L1");
            await access.GetFormLayoutAsync("L1"); // 第二次命中本地快取，不打 connector
            Assert.Equal(1, connector.GetDefineCallCount);

            // 模擬 EnterCompany 切換公司導致 customizeId 變動後的清快取。
            access.ClearCache();

            await access.GetFormLayoutAsync("L1"); // 快取已清，必須重新抓取
            Assert.Equal(2, connector.GetDefineCallCount);
        }

        [Fact]
        [DisplayName("ClearCache 不影響後續正常快取行為")]
        public async Task ClearCache_DoesNotBreakSubsequentCaching()
        {
            var connector = new CountingConnector();
            var access = new ClientDefineAccess(connector);

            await access.GetFormLayoutAsync("L1");
            access.ClearCache();
            await access.GetFormLayoutAsync("L1");
            await access.GetFormLayoutAsync("L1"); // 清快取後重新抓一次，之後仍走快取
            Assert.Equal(2, connector.GetDefineCallCount);
        }

        [Fact]
        [DisplayName("失敗的抓取不應污染快取，下次讀取應重新嘗試")]
        public async Task GetFormLayoutAsync_FailedFetch_IsEvictedAndRetried()
        {
            var connector = new FlakyConnector();
            var access = new ClientDefineAccess(connector);

            // 第一次抓取拋例外，不得快取 faulted task。
            await Assert.ThrowsAsync<InvalidOperationException>(() => access.GetFormLayoutAsync("L1"));
            // 第二次（connector 已恢復）應重新抓取並成功。
            var result = await access.GetFormLayoutAsync("L1");

            Assert.NotNull(result);
            Assert.Equal(2, connector.GetDefineCallCount);
        }

        [Fact]
        [DisplayName("ClientDefineAccess 底層 connector 拋例外應原樣傳遞，非 AggregateException")]
        public async Task GetSystemSettingsAsync_PropagatesConnectorException()
        {
            // 指向不可達 endpoint：HTTP 呼叫必然失敗，觸發 connector 端例外。
            var connector = new SystemApiConnector("http://127.0.0.1:1/", Guid.NewGuid());
            var access = new ClientDefineAccess(connector);

            // async/await 解包後原例外應原樣傳出，而非 AggregateException。
            var ex = await Assert.ThrowsAnyAsync<Exception>(() => access.GetSystemSettingsAsync());
            Assert.IsNotType<AggregateException>(ex);
        }

        /// <summary>
        /// Spy connector that counts <see cref="SystemApiConnector.GetDefineAsync{T}"/> calls and
        /// returns a fresh instance, so cache hits (no call) vs misses (call) are observable without
        /// touching a real server.
        /// </summary>
        private sealed class CountingConnector : SystemApiConnector
        {
            public CountingConnector() : base(Guid.NewGuid()) { }

            public int GetDefineCallCount { get; private set; }

            public override Task<T> GetDefineAsync<T>(DefineType defineType, string[]? keys = null)
            {
                GetDefineCallCount++;
                return Task.FromResult(Activator.CreateInstance<T>());
            }
        }

        /// <summary>
        /// Spy connector whose <see cref="GetDefineAsync{T}"/> blocks until <see cref="Release"/> is
        /// called, so two concurrent misses can be observed sharing a single in-flight fetch.
        /// </summary>
        private sealed class GatedConnector : SystemApiConnector
        {
            private readonly TaskCompletionSource<bool> _gate =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public GatedConnector() : base(Guid.NewGuid()) { }

            public int GetDefineCallCount { get; private set; }

            public void Release() => _gate.TrySetResult(true);

            public override async Task<T> GetDefineAsync<T>(DefineType defineType, string[]? keys = null)
            {
                GetDefineCallCount++;
                await _gate.Task.ConfigureAwait(false);
                return Activator.CreateInstance<T>();
            }
        }

        /// <summary>
        /// Spy connector that throws on its first call and succeeds afterwards, so failure eviction
        /// (a faulted fetch must not poison the cache) is observable.
        /// </summary>
        private sealed class FlakyConnector : SystemApiConnector
        {
            public FlakyConnector() : base(Guid.NewGuid()) { }

            public int GetDefineCallCount { get; private set; }

            public override Task<T> GetDefineAsync<T>(DefineType defineType, string[]? keys = null)
            {
                GetDefineCallCount++;
                if (GetDefineCallCount == 1)
                    throw new InvalidOperationException("Simulated transient fetch failure.");
                return Task.FromResult(Activator.CreateInstance<T>());
            }
        }
    }
}
