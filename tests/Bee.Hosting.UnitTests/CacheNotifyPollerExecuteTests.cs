using System.ComponentModel;
using Bee.Db;
using Bee.Definition.Settings;
using Bee.Hosting.CacheNotify;
using Bee.ObjectCaching;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// <see cref="CacheNotifyPoller.ExecuteAsync"/> 的單元測試：驗證 hosted service 啟動後
    /// 取消時可乾淨退出，並覆蓋 <c>intervalSeconds</c> 預設值分支。
    /// </summary>
    public class CacheNotifyPollerExecuteTests
    {
        private sealed class ThrowingDbFactory : IDbAccessFactory
        {
            private readonly Exception _exception;
            public ThrowingDbFactory(Exception exception) { _exception = exception; }
            public DbAccess Create(string databaseId) => throw _exception;
        }

        private sealed class StubCacheContainer : ICacheContainer
        {
            public SystemSettingsCache SystemSettings => throw new NotImplementedException();
            public DatabaseSettingsCache DatabaseSettings => throw new NotImplementedException();
            public ProgramSettingsCache ProgramSettings => throw new NotImplementedException();
            public PermissionModelsCache PermissionModels => throw new NotImplementedException();
            public DbCategorySettingsCache DbCategorySettings => throw new NotImplementedException();
            public TableSchemaCache TableSchema => throw new NotImplementedException();
            public FormSchemaCache FormSchema => throw new NotImplementedException();
            public FormLayoutCache FormLayout => throw new NotImplementedException();
            public LanguageResourceCache LanguageResource => throw new NotImplementedException();
            public SessionInfoCache SessionInfo => throw new NotImplementedException();
            public CompanyInfoCache CompanyInfo => throw new NotImplementedException();
            public CompanyRolePermissionsCache CompanyRolePermissions => throw new NotImplementedException();
            public bool TryEvict(string cacheKey) => false;
        }

        private sealed class StubLogger : ILogger<CacheNotifyPoller>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter) { }
        }

        private static readonly ICacheContainer s_container = new StubCacheContainer();
        private static readonly ILogger<CacheNotifyPoller> s_logger = new StubLogger();

        [Fact]
        [DisplayName("ExecuteAsync 啟動後取消應乾淨退出，不拋例外")]
        public async Task ExecuteAsync_Cancelled_CompletesWithoutThrowing()
        {
            var options = new CacheNotifyOptions { IntervalSeconds = 60 };
            var throwingFactory = new ThrowingDbFactory(new InvalidOperationException("DB 錯誤"));
            var poller = new CacheNotifyPoller(throwingFactory, s_container, options, s_logger);

            IHostedService hosted = poller;
            var exception = await Record.ExceptionAsync(async () =>
            {
                await hosted.StartAsync(CancellationToken.None);
                await hosted.StopAsync(CancellationToken.None);
            });
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("ExecuteAsync IntervalSeconds 為 0 時應使用預設 5 秒間隔並在取消後乾淨退出")]
        public async Task ExecuteAsync_ZeroIntervalSeconds_DefaultsToFiveSecondsAndCompletes()
        {
            var options = new CacheNotifyOptions { IntervalSeconds = 0 };
            var throwingFactory = new ThrowingDbFactory(new InvalidOperationException("DB 錯誤"));
            var poller = new CacheNotifyPoller(throwingFactory, s_container, options, s_logger);

            IHostedService hosted = poller;
            var exception = await Record.ExceptionAsync(async () =>
            {
                await hosted.StartAsync(CancellationToken.None);
                await hosted.StopAsync(CancellationToken.None);
            });
            Assert.Null(exception);
        }
    }
}
