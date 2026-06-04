using System.ComponentModel;
using System.Data.Common;
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
    /// <see cref="CacheNotifyPoller.ExecuteAsync"/> 的輕量單元測試。
    /// 以已取消的 <see cref="CancellationToken"/> 確保 timer 不實際等待，
    /// 驗證方法能正常完成（baseline SafePoll 執行後因 token 取消而結束）。
    /// </summary>
    public class CacheNotifyPollerExecuteTests
    {
        private sealed class ThrowingDbFactory : IDbAccessFactory
        {
            private readonly Exception _exception;
            public ThrowingDbFactory(Exception exception) => _exception = exception;
            public DbAccess Create(string databaseId) => throw _exception;
        }

        private sealed class FakeDbException : DbException
        {
            public FakeDbException(string message) : base(message) { }
        }

        private sealed class SilentLogger : ILogger<CacheNotifyPoller>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter) { }
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

        private static readonly ICacheContainer s_container = new StubCacheContainer();
        private static readonly ILogger<CacheNotifyPoller> s_logger = new SilentLogger();

        private static Task InvokeExecuteAsync(CacheNotifyPoller poller, CancellationToken token)
        {
            var method = typeof(CacheNotifyPoller).GetMethod(
                "ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return (Task)method!.Invoke(poller, [token])!;
        }

        [Fact]
        [DisplayName("ExecuteAsync 以已取消的 token 啟動時應在 baseline Poll 後立即結束，不拋例外")]
        public async Task ExecuteAsync_PreCancelledToken_CompletesWithoutError()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var factory = new ThrowingDbFactory(new FakeDbException("baseline poll db error"));
            var options = new CacheNotifyOptions { IntervalSeconds = 5 };
            var poller = new CacheNotifyPoller(factory, s_container, options, s_logger);

            var task = InvokeExecuteAsync(poller, cts.Token);
            var exception = await Record.ExceptionAsync(() => task);

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("ExecuteAsync IntervalSeconds 為 0 時應以預設 5 秒執行（以已取消 token 驗證不拋例外）")]
        public async Task ExecuteAsync_IntervalSecondsZero_DefaultsToFive_CompletesWithoutError()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var factory = new ThrowingDbFactory(new FakeDbException("db error"));
            var options = new CacheNotifyOptions { IntervalSeconds = 0 };
            var poller = new CacheNotifyPoller(factory, s_container, options, s_logger);

            var task = InvokeExecuteAsync(poller, cts.Token);
            var exception = await Record.ExceptionAsync(() => task);

            Assert.Null(exception);
        }
    }
}
