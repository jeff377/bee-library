using System.ComponentModel;
using System.Data.Common;
using Bee.Definition.Identity;
using Bee.Definition.Settings;
using Bee.Hosting.Session;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Abstractions.System;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// <see cref="ExpiredSessionCleanupService"/> 的行為測試。
    /// </summary>
    /// <remarks>
    /// 這是框架內**唯一會刪資料**的背景服務，先前零覆蓋。三件事值得釘住：啟動時就掃一次
    /// （不是等第一個 tick）、暫時性 DB 錯誤不得終結迴圈（否則表會長到行程結束）、
    /// 以及 <c>IntervalSeconds &lt;= 0</c> 要退回 3600 秒而不是變成忙迴圈。
    /// </remarks>
    public class ExpiredSessionCleanupServiceTests
    {
        /// <summary>記錄呼叫次數，並可依次數決定是否擲例外。</summary>
        private sealed class RecordingSessionRepository : ISessionRepository
        {
            private readonly Func<int, int> _behavior;
            private int _calls;

            public RecordingSessionRepository(Func<int, int> behavior) { _behavior = behavior; }

            public int Calls => Volatile.Read(ref _calls);

            public int DeleteExpiredSessions()
            {
                var n = Interlocked.Increment(ref _calls);
                return _behavior(n);
            }

            public SessionUser? GetSession(Guid accessToken) => throw new NotSupportedException();
            public void InsertSession(SessionUser sessionUser) => throw new NotSupportedException();
            public void UpdateSession(SessionUser sessionUser) => throw new NotSupportedException();
            public void DeleteSession(Guid accessToken) => throw new NotSupportedException();
        }

        private sealed class StubRepositoryFactory : IRepositoryFactory
        {
            private readonly ISessionRepository _repository;
            public StubRepositoryFactory(ISessionRepository repository) { _repository = repository; }

            public T Create<T>(Guid accessToken = default) where T : class => (T)_repository;

            public T CreateFormRepository<T>(Guid accessToken, string progId)
                where T : class, IDataFormRepository => throw new NotSupportedException();
        }

        /// <summary><see cref="DbException"/> 是抽象的，測試需要一個具體的可擲型別。</summary>
        private sealed class FakeDbException : DbException
        {
            public FakeDbException() : base("simulated transient database failure") { }
        }

        private sealed class StubLogger : ILogger<ExpiredSessionCleanupService>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter) { }
        }

        private static ExpiredSessionCleanupService Create(
            RecordingSessionRepository repository, int intervalSeconds) =>
            new(new StubRepositoryFactory(repository),
                new SessionCleanupOptions { Enabled = true, IntervalSeconds = intervalSeconds },
                new StubLogger());

        /// <summary>輪詢等待，避免以固定 sleep 換取穩定度。</summary>
        private static async Task<bool> WaitForCallsAsync(
            RecordingSessionRepository repository, int expected, int timeoutMs = 5000)
        {
            var deadline = Environment.TickCount64 + timeoutMs;
            while (Environment.TickCount64 < deadline)
            {
                if (repository.Calls >= expected) { return true; }
                await Task.Delay(20).ConfigureAwait(false);
            }
            return repository.Calls >= expected;
        }

        [Fact]
        [DisplayName("啟動時應立即掃一次，而非等到第一個 tick")]
        public async Task StartAsync_SweepsImmediately()
        {
            // 間隔刻意設得極長：若啟動掃描不存在，這個測試就只能靠等 1 小時才會過。
            var repository = new RecordingSessionRepository(_ => 3);
            var service = Create(repository, intervalSeconds: 3600);

            await service.StartAsync(CancellationToken.None);
            try
            {
                Assert.True(await WaitForCallsAsync(repository, 1), "啟動後未觀察到任何一次清理。");
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
                service.Dispose();
            }
        }

        [Fact]
        [DisplayName("暫時性 DbException 不得終結迴圈，下一個 tick 仍應繼續掃描")]
        public async Task DbException_DoesNotEndTheLoop()
        {
            // 前兩次擲 DbException：第一次是啟動掃描、第二次是第一個 tick。
            // 迴圈若被例外終結，第三次呼叫永遠不會發生。
            var repository = new RecordingSessionRepository(n =>
                n <= 2 ? throw new FakeDbException() : 0);
            var service = Create(repository, intervalSeconds: 1);

            await service.StartAsync(CancellationToken.None);
            try
            {
                Assert.True(await WaitForCallsAsync(repository, 3),
                    $"迴圈在 DbException 後停止了；只觀察到 {repository.Calls} 次呼叫。");
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
                service.Dispose();
            }
        }

        [Fact]
        [DisplayName("IntervalSeconds 為 0 應退回預設 3600 秒，不得變成忙迴圈")]
        public async Task ZeroInterval_FallsBackToDefault_AndDoesNotSpin()
        {
            var repository = new RecordingSessionRepository(_ => 0);
            var service = Create(repository, intervalSeconds: 0);

            await service.StartAsync(CancellationToken.None);
            try
            {
                Assert.True(await WaitForCallsAsync(repository, 1), "啟動後未觀察到任何一次清理。");

                // 退回值是 3600 秒，故這段觀察窗內不應再有第二次。若 fallback 失效
                // （PeriodicTimer 對 TimeSpan.Zero 會擲例外，或間隔變成 0），這裡會看到暴增。
                await Task.Delay(600);
                Assert.Equal(1, repository.Calls);
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
                service.Dispose();
            }
        }

        [Fact]
        [DisplayName("停止服務不應拋出例外（取消是正常關機路徑）")]
        public async Task StopAsync_CompletesWithoutThrowing()
        {
            var repository = new RecordingSessionRepository(_ => 0);
            var service = Create(repository, intervalSeconds: 1);
            await service.StartAsync(CancellationToken.None);

            var exception = await Record.ExceptionAsync(() => service.StopAsync(CancellationToken.None));

            service.Dispose();
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("非 DbException 的例外不應被吞掉（那不是這個 catch 要處理的失敗）")]
        public async Task NonDbException_IsNotSwallowed()
        {
            var repository = new RecordingSessionRepository(_ => throw new InvalidOperationException("bug"));
            var service = Create(repository, intervalSeconds: 3600);

            await service.StartAsync(CancellationToken.None);

            // 斷言在 `ExecuteTask` 而非 `StartAsync` 上：`BackgroundService` 是否把已完成的
            // 執行工作交還給 `StartAsync`，屬於它的內部政策，實測在此並未傳播出來。要證的是
            // 「這個例外沒有被 SafeCleanup 的 catch (DbException) 吞掉」，`ExecuteTask` 的
            // 狀態才是該事實的直接證據。
            var executeTask = service.ExecuteTask;
            Assert.NotNull(executeTask);
            var exception = await Record.ExceptionAsync(() => executeTask!);

            service.Dispose();
            Assert.IsType<InvalidOperationException>(exception);
        }
    }
}
