using System.ComponentModel;
using System.Data.Common;
using System.Reflection;
using Bee.Db;
using Bee.Definition.Settings;
using Bee.Hosting.CacheNotify;
using Bee.ObjectCaching;
using Bee.ObjectCaching.CacheNotify;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 純單元測試：覆蓋 <see cref="CacheNotifyPoller"/> 建構子 null 檢查
    /// 與 <c>SafePoll</c> 例外吞噬行為，無需連接真實資料庫。
    /// </summary>
    public class CacheNotifyPollerUnitTests
    {
        private static readonly CacheNotifyOptions s_options = new() { IntervalSeconds = 1 };
        private static readonly ILogger<CacheNotifyPoller> s_logger = NullLogger<CacheNotifyPoller>.Instance;

        private sealed class StubDbFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new InvalidOperationException("stub");
        }

        private sealed class ThrowingDbFactory : IDbAccessFactory
        {
            private readonly Exception _exception;
            public ThrowingDbFactory(Exception exception) => _exception = exception;
            public DbAccess Create(string databaseId) => throw _exception;
        }

        private sealed class StubContainer : ICacheContainer
        {
            public SystemSettingsCache SystemSettings => null!;
            public DatabaseSettingsCache DatabaseSettings => null!;
            public ProgramSettingsCache ProgramSettings => null!;
            public DbCategorySettingsCache DbCategorySettings => null!;
            public TableSchemaCache TableSchema => null!;
            public FormSchemaCache FormSchema => null!;
            public FormLayoutCache FormLayout => null!;
            public LanguageResourceCache LanguageResource => null!;
            public SessionInfoCache SessionInfo => null!;
            public CompanyInfoCache CompanyInfo => null!;
        }

        private sealed class StubRouter : ICacheNotifyRouter
        {
            public void Register(string cacheGroup, Action<ICacheContainer, string> evict) { }
            public bool TryInvoke(ICacheContainer container, string cacheKey) => false;
        }

        private sealed class TestDbException : DbException
        {
            public TestDbException(string message) : base(message) { }
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 建構子傳入 null dbAccessFactory 應拋 ArgumentNullException")]
        public void Constructor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(null!, new StubContainer(), new StubRouter(), s_options, s_logger));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 建構子傳入 null container 應拋 ArgumentNullException")]
        public void Constructor_NullContainer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(new StubDbFactory(), null!, new StubRouter(), s_options, s_logger));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 建構子傳入 null router 應拋 ArgumentNullException")]
        public void Constructor_NullRouter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(new StubDbFactory(), new StubContainer(), null!, s_options, s_logger));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 建構子傳入 null options 應拋 ArgumentNullException")]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(new StubDbFactory(), new StubContainer(), new StubRouter(), null!, s_logger));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 建構子傳入 null logger 應拋 ArgumentNullException")]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(new StubDbFactory(), new StubContainer(), new StubRouter(), s_options, null!));
        }

        private static Task RunExecuteAsync(CacheNotifyPoller poller, CancellationToken cancellationToken)
        {
            var method = typeof(CacheNotifyPoller).GetMethod(
                "ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Task)method!.Invoke(poller, new object[] { cancellationToken })!;
        }

        [Fact]
        [DisplayName("SafePoll 遇到 DbException 應捕捉例外、不向外拋（輪詢不中斷）")]
        public async Task ExecuteAsync_SafePollThrowsDbException_ExceptionSwallowed()
        {
            var factory = new ThrowingDbFactory(new TestDbException("db error"));
            var poller = new CacheNotifyPoller(factory, new StubContainer(), new StubRouter(), s_options, s_logger);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var exception = await Record.ExceptionAsync(() => RunExecuteAsync(poller, cts.Token));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("SafePoll 遇到 InvalidOperationException 應捕捉例外、不向外拋（輪詢不中斷）")]
        public async Task ExecuteAsync_SafePollThrowsInvalidOperationException_ExceptionSwallowed()
        {
            var factory = new ThrowingDbFactory(new InvalidOperationException("invalid op"));
            var poller = new CacheNotifyPoller(factory, new StubContainer(), new StubRouter(), s_options, s_logger);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var exception = await Record.ExceptionAsync(() => RunExecuteAsync(poller, cts.Token));
            Assert.Null(exception);
        }
    }
}
