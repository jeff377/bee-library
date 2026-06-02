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
    /// <see cref="CacheNotifyPoller"/> 建構式的 null 引數驗證測試。
    /// 不依賴資料庫，覆蓋 0% 覆蓋率的建構式程式碼。
    /// </summary>
    public class CacheNotifyPollerConstructorTests
    {
        private static FakeDbAccessFactory ValidFactory => new();
        private static FakeCacheContainer ValidContainer => new();
        private static CacheNotifyOptions ValidOptions => new();
        private static ILogger<CacheNotifyPoller> ValidLogger => new FakeLogger<CacheNotifyPoller>();

        [Fact]
        [DisplayName("CacheNotifyPoller 傳入 null dbAccessFactory 應拋 ArgumentNullException")]
        public void Constructor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(null!, ValidContainer, ValidOptions, ValidLogger));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 傳入 null container 應拋 ArgumentNullException")]
        public void Constructor_NullContainer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(ValidFactory, null!, ValidOptions, ValidLogger));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 傳入 null options 應拋 ArgumentNullException")]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(ValidFactory, ValidContainer, null!, ValidLogger));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 傳入 null logger 應拋 ArgumentNullException")]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(ValidFactory, ValidContainer, ValidOptions, null!));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 傳入所有有效引數應成功建構，不拋例外")]
        public void Constructor_AllValidArguments_DoesNotThrow()
        {
            var exception = Record.Exception(() =>
                new CacheNotifyPoller(ValidFactory, ValidContainer, ValidOptions, ValidLogger));
            Assert.Null(exception);
        }

        private sealed class FakeDbAccessFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new NotImplementedException();
        }

        private sealed class FakeCacheContainer : ICacheContainer
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
            public bool TryEvict(string cacheKey) => false;
        }

        private sealed class FakeLogger<T> : ILogger<T>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}
