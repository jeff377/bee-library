using Microsoft.Extensions.Logging;

namespace Bee.Business.UnitTests.Fakes
{
    /// <summary>
    /// 記下所有 logger 寫出的項目，供驗證「失敗被吞下時確實留下可觀測的記錄」。
    /// </summary>
    internal sealed class RecordingLoggerFactory : ILoggerFactory
    {
        private readonly List<LogRecord> _entries = [];

        /// <summary>目前為止記下的項目快照。</summary>
        public IReadOnlyList<LogRecord> Entries
        {
            get { lock (_entries) { return [.. _entries]; } }
        }

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(this, categoryName);

        public void AddProvider(ILoggerProvider provider)
        {
            // Every logger this factory creates records into the factory itself; providers are not used.
        }

        public void Dispose()
        {
            // Holds no resources.
        }

        private void Add(LogRecord record)
        {
            lock (_entries) { _entries.Add(record); }
        }

        /// <summary>一筆記錄。</summary>
        public sealed record LogRecord(string Category, LogLevel Level, string Message, Exception? Exception);

        private sealed class RecordingLogger(RecordingLoggerFactory owner, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
                => owner.Add(new LogRecord(category, logLevel, formatter(state, exception), exception));
        }
    }
}
