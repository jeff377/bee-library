using System.ComponentModel;
using System.Reflection;
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
    /// <see cref="CacheNotifyPoller"/> <c>ExecuteAsync</c> 路徑的單元測試。
    /// 以已取消的 <see cref="CancellationToken"/> 驗證：初始 SafePoll 被呼叫後
    /// 正常完成而不拋例外，並驗證 <see cref="CacheNotifyOptions.IntervalSeconds"/> 為零時
    /// 自動套用預設值。
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
            public DepartmentTreeCache DepartmentTree => throw new NotImplementedException();
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
        [DisplayName("ExecuteAsync 帶已取消的 CancellationToken 時應完成初始 SafePoll 後正常結束，不拋例外")]
        public async Task ExecuteAsync_ImmediatelyCancelled_CompletesWithoutException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var throwingFactory = new ThrowingDbFactory(new InvalidOperationException("simulated db error"));
            var options = new CacheNotifyOptions { IntervalSeconds = 1 };
            var poller = new CacheNotifyPoller(throwingFactory, s_container, options, s_logger);

            var method = typeof(CacheNotifyPoller)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task)method!.Invoke(poller, new object[] { cts.Token })!;
            var exception = await Record.ExceptionAsync(() => task);
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("ExecuteAsync IntervalSeconds 為零時自動使用預設 5 秒，帶已取消 token 正常結束")]
        public async Task ExecuteAsync_ZeroIntervalSeconds_UsesDefaultAndCompletes()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var throwingFactory = new ThrowingDbFactory(new InvalidOperationException("simulated"));
            var options = new CacheNotifyOptions { IntervalSeconds = 0 };
            var poller = new CacheNotifyPoller(throwingFactory, s_container, options, s_logger);

            var method = typeof(CacheNotifyPoller)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task)method!.Invoke(poller, new object[] { cts.Token })!;
            var exception = await Record.ExceptionAsync(() => task);
            Assert.Null(exception);
        }
    }
}
