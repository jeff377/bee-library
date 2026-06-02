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
    /// 純單元測試：驗證 <see cref="CacheNotifyPoller"/> 建構子的 null 引數防護。
    /// 不依賴資料庫，不依賴 DI 容器。
    /// </summary>
    public class CacheNotifyPollerCtorTests
    {
        private sealed class StubDbAccessFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new NotSupportedException();
        }

        private sealed class StubCacheContainer : ICacheContainer
        {
            public SystemSettingsCache SystemSettings => throw new NotSupportedException();
            public DatabaseSettingsCache DatabaseSettings => throw new NotSupportedException();
            public ProgramSettingsCache ProgramSettings => throw new NotSupportedException();
            public DbCategorySettingsCache DbCategorySettings => throw new NotSupportedException();
            public TableSchemaCache TableSchema => throw new NotSupportedException();
            public FormSchemaCache FormSchema => throw new NotSupportedException();
            public FormLayoutCache FormLayout => throw new NotSupportedException();
            public LanguageResourceCache LanguageResource => throw new NotSupportedException();
            public SessionInfoCache SessionInfo => throw new NotSupportedException();
            public CompanyInfoCache CompanyInfo => throw new NotSupportedException();
            public bool TryEvict(string cacheKey) => false;
        }

        private sealed class StubLogger : ILogger<CacheNotifyPoller>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }

        private static IDbAccessFactory NewFactory() => new StubDbAccessFactory();
        private static ICacheContainer NewContainer() => new StubCacheContainer();
        private static CacheNotifyOptions NewOptions() => new CacheNotifyOptions();
        private static ILogger<CacheNotifyPoller> NewLogger() => new StubLogger();

        [Fact]
        [DisplayName("CacheNotifyPoller 傳入 null IDbAccessFactory 應拋 ArgumentNullException")]
        public void Ctor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(null!, NewContainer(), NewOptions(), NewLogger()));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 傳入 null ICacheContainer 應拋 ArgumentNullException")]
        public void Ctor_NullCacheContainer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(NewFactory(), null!, NewOptions(), NewLogger()));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 傳入 null CacheNotifyOptions 應拋 ArgumentNullException")]
        public void Ctor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(NewFactory(), NewContainer(), null!, NewLogger()));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 傳入 null ILogger 應拋 ArgumentNullException")]
        public void Ctor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(NewFactory(), NewContainer(), NewOptions(), null!));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 全部有效參數應可正常建立實例，不拋例外")]
        public void Ctor_AllValidArgs_CreatesInstance()
        {
            var exception = Record.Exception(() =>
                new CacheNotifyPoller(NewFactory(), NewContainer(), NewOptions(), NewLogger()));
            Assert.Null(exception);
        }
    }
}
