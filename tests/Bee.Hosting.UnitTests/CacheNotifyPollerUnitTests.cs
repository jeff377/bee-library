using System.ComponentModel;
using Bee.Db;
using Bee.Definition.Settings;
using Bee.Hosting.CacheNotify;
using Bee.ObjectCaching;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// <see cref="CacheNotifyPoller"/> 建構子守衛與 ExecuteAsync 容錯行為的單元測試。
    /// 不依賴真實資料庫——以 stub 讓 SafePoll 觸發並確認例外被吸收。
    /// </summary>
    public class CacheNotifyPollerUnitTests
    {
        private sealed class StubDbAccessFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) =>
                throw new InvalidOperationException("Stub：無真實資料庫連線。");
        }

        private sealed class StubCacheContainer : ICacheContainer
        {
            public SystemSettingsCache SystemSettings => throw new NotImplementedException();
            public DatabaseSettingsCache DatabaseSettings => throw new NotImplementedException();
            public ProgramSettingsCache ProgramSettings => throw new NotImplementedException();
            public DbCategorySettingsCache DbCategorySettings => throw new NotImplementedException();
            public TableSchemaCache TableSchema => throw new NotImplementedException();
            public FormSchemaCache FormSchema => throw new NotImplementedException();
            public FormLayoutCache FormLayout => throw new NotImplementedException();
            public LanguageResourceCache LanguageResource => throw new NotImplementedException();
            public SessionInfoCache SessionInfo => throw new NotImplementedException();
            public CompanyInfoCache CompanyInfo => throw new NotImplementedException();
            public bool TryEvict(string cacheKey) => false;
        }

        [Fact]
        [DisplayName("建構子 dbAccessFactory 為 null 應拋 ArgumentNullException")]
        public void Ctor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheNotifyPoller(
                null!,
                new StubCacheContainer(),
                new CacheNotifyOptions(),
                NullLogger<CacheNotifyPoller>.Instance));
        }

        [Fact]
        [DisplayName("建構子 container 為 null 應拋 ArgumentNullException")]
        public void Ctor_NullContainer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheNotifyPoller(
                new StubDbAccessFactory(),
                null!,
                new CacheNotifyOptions(),
                NullLogger<CacheNotifyPoller>.Instance));
        }

        [Fact]
        [DisplayName("建構子 options 為 null 應拋 ArgumentNullException")]
        public void Ctor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheNotifyPoller(
                new StubDbAccessFactory(),
                new StubCacheContainer(),
                null!,
                NullLogger<CacheNotifyPoller>.Instance));
        }

        [Fact]
        [DisplayName("建構子 logger 為 null 應拋 ArgumentNullException")]
        public void Ctor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheNotifyPoller(
                new StubDbAccessFactory(),
                new StubCacheContainer(),
                new CacheNotifyOptions(),
                null!));
        }

        [Fact]
        [DisplayName("ExecuteAsync SafePoll 中例外應被吸收，StopAsync 應正常完成")]
        public async Task ExecuteAsync_InvalidOperationExceptionInPoll_StopAsyncCompletesWithoutException()
        {
            var poller = new CacheNotifyPoller(
                new StubDbAccessFactory(),
                new StubCacheContainer(),
                new CacheNotifyOptions { DatabaseId = "test", IntervalSeconds = 60 },
                NullLogger<CacheNotifyPoller>.Instance);

            using var cts = new CancellationTokenSource();
            await poller.StartAsync(cts.Token);

            var exception = await Record.ExceptionAsync(() => poller.StopAsync(CancellationToken.None));
            Assert.Null(exception);
        }
    }
}
