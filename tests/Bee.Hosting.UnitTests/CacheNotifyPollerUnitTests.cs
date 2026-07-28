using System.ComponentModel;
using System.Data.Common;
using System.Reflection;
using Bee.Db.CacheNotify;
using Bee.Definition.Settings;
using Bee.Hosting.CacheNotify;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// <see cref="CacheNotifyPoller"/> 建構子防衛式斷言與 <c>SafePoll</c> 例外吞除邏輯的單元測試。
    /// </summary>
    public class CacheNotifyPollerUnitTests
    {
        private sealed class StubReader : ICacheNotifyReader
        {
            public DateTime ReadBaseline(string databaseId) => throw new NotImplementedException();

            public IReadOnlyList<CacheNotifyChange> ReadChangesSince(string databaseId, DateTime threshold)
                => throw new NotImplementedException();
        }

        private sealed class ThrowingReader : ICacheNotifyReader
        {
            private readonly Exception _exception;
            public ThrowingReader(Exception exception) { _exception = exception; }

            public DateTime ReadBaseline(string databaseId) => throw _exception;

            public IReadOnlyList<CacheNotifyChange> ReadChangesSince(string databaseId, DateTime threshold)
                => throw _exception;
        }

        private sealed class FakeDbException : DbException
        {
            public FakeDbException(string message) : base(message) { }
        }

        private sealed class StubLogger : ILogger<CacheNotifyPoller>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) { }
        }

        private static readonly ICacheNotifyReader s_reader = new StubReader();
        private static readonly CacheNotifyOptions s_options = new();
        private static readonly ILogger<CacheNotifyPoller> s_logger = new StubLogger();

        [Fact]
        [DisplayName("CacheNotifyPoller 建構子所有參數有效時應成功建立實例，不拋例外")]
        public void Constructor_ValidArguments_CreatesInstance()
        {
            var exception = Record.Exception(() =>
                new CacheNotifyPoller(s_reader, s_options, s_logger));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 建構子 reader 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullReader_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(null!, s_options, s_logger));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 建構子 options 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(s_reader, null!, s_logger));
        }

        [Fact]
        [DisplayName("CacheNotifyPoller 建構子 logger 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPoller(s_reader, s_options, null!));
        }

        [Fact]
        [DisplayName("SafePoll session.Poll() 拋 InvalidOperationException 時應被吞除，不向外傳播")]
        public void SafePoll_SessionThrowsInvalidOperationException_DoesNotPropagate()
        {
            var throwingReader = new ThrowingReader(new InvalidOperationException("simulated db error"));
            var session = new CacheNotifyPollSession("test_db", throwingReader, marginSeconds: 0);
            var poller = new CacheNotifyPoller(s_reader, s_options, s_logger);

            var safePollMethod = typeof(CacheNotifyPoller).GetMethod(
                "SafePoll", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(safePollMethod);

            var exception = Record.Exception(() => safePollMethod!.Invoke(poller, new object[] { session }));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("SafePoll session.Poll() 拋 DbException 時應被吞除，不向外傳播")]
        public void SafePoll_SessionThrowsDbException_DoesNotPropagate()
        {
            var throwingReader = new ThrowingReader(new FakeDbException("simulated db provider exception"));
            var session = new CacheNotifyPollSession("test_db", throwingReader, marginSeconds: 0);
            var poller = new CacheNotifyPoller(s_reader, s_options, s_logger);

            var safePollMethod = typeof(CacheNotifyPoller).GetMethod(
                "SafePoll", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(safePollMethod);

            var exception = Record.Exception(() => safePollMethod!.Invoke(poller, new object[] { session }));
            Assert.Null(exception);
        }
    }
}
