using System.ComponentModel;
using System.Reflection;
using Bee.Db;
using Bee.Definition.Settings;
using Bee.Hosting.CacheNotify;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// <see cref="CacheNotifyPoller.ExecuteAsync"/> 的單元測試：驗證計時迴圈在
    /// CancellationToken 取消後能正常結束，以及 IntervalSeconds 為 0 時自動修正為 5 秒。
    /// </summary>
    public class CacheNotifyPollerExecuteAsyncTests
    {
        private sealed class ThrowingDbFactory : IDbAccessFactory
        {
            private readonly Exception _exception;
            public ThrowingDbFactory(Exception exception) { _exception = exception; }
            public DbAccess Create(string databaseId) => throw _exception;
        }

        private sealed class StubLogger : ILogger<CacheNotifyPoller>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter) { }
        }

        private static readonly ILogger<CacheNotifyPoller> s_logger = new StubLogger();

        private static CacheNotifyPoller MakePoller(CacheNotifyOptions options)
        {
            var factory = new ThrowingDbFactory(new InvalidOperationException("sim poll error"));
            return new CacheNotifyPoller(factory, options, s_logger);
        }

        private static async Task InvokeExecuteAsync(CacheNotifyPoller poller, CancellationToken token)
        {
            var method = typeof(CacheNotifyPoller).GetMethod(
                "ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(poller, new object[] { token })!;
            await task;
        }

        [Fact]
        [DisplayName("ExecuteAsync 收到已取消的 CancellationToken 時應正常完成，不拋例外")]
        public async Task ExecuteAsync_PreCancelledToken_CompletesNormally()
        {
            var poller = MakePoller(new CacheNotifyOptions());
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var exception = await Record.ExceptionAsync(() => InvokeExecuteAsync(poller, cts.Token));

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("ExecuteAsync IntervalSeconds 為 0 時應修正為 5 秒間隔並正常完成")]
        public async Task ExecuteAsync_ZeroIntervalSeconds_UsesDefaultFiveSeconds_CompletesNormally()
        {
            var options = new CacheNotifyOptions { IntervalSeconds = 0 };
            var poller = MakePoller(options);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var exception = await Record.ExceptionAsync(() => InvokeExecuteAsync(poller, cts.Token));

            Assert.Null(exception);
        }
    }
}
