using System.ComponentModel;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Hosting.Audit;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// <see cref="AuditLogWriterService"/> 的單元測試：驗證佇列滿載時退化為同步寫入（不丟失），
    /// 以及背景服務啟停能把入列項目批次寫入 sink。
    /// </summary>
    public class AuditLogWriterServiceTests
    {
        private sealed class FakeAuditLogSink : IAuditLogSink
        {
            private readonly object _lock = new();
            private readonly List<AuditEntry> _entries = [];

            public int Count
            {
                get { lock (_lock) { return _entries.Count; } }
            }

            public List<AuditEntry> Snapshot()
            {
                lock (_lock) { return [.. _entries]; }
            }

            public void WriteBatch(IReadOnlyList<AuditEntry> entries)
            {
                lock (_lock)
                {
                    _entries.AddRange(entries);
                }
            }
        }

        private sealed class TestAuditEntry : AuditEntry
        {
            public override string TableName => "st_log_test";

            protected override void AddColumns(IList<AuditColumn> columns)
            {
                // No axis-specific columns needed for these tests.
            }
        }

        [Fact]
        [DisplayName("佇列滿載時 Write 應退化為同步寫入，不丟失項目")]
        public void Write_QueueFull_FallsBackToSynchronous()
        {
            var sink = new FakeAuditLogSink();
            var options = new AuditLogOptions { QueueCapacity = 1 };
            using var service = new AuditLogWriterService(sink, options);

            var first = new TestAuditEntry();
            var second = new TestAuditEntry();

            // The service is not started, so nothing drains the queue: the first entry fills the
            // bounded queue and the second overflows into the synchronous fallback.
            service.Write(first);
            service.Write(second);

            var written = sink.Snapshot();
            Assert.Single(written);
            Assert.Same(second, written[0]);
        }

        [Fact]
        [DisplayName("背景服務啟動後入列項目應被寫入，停止時清空緩衝")]
        public async Task BackgroundDrain_WritesEnqueuedEntries()
        {
            var sink = new FakeAuditLogSink();
            var options = new AuditLogOptions { QueueCapacity = 100, BatchSize = 10 };
            using var service = new AuditLogWriterService(sink, options);

            await service.StartAsync(CancellationToken.None);
            try
            {
                var first = new TestAuditEntry();
                var second = new TestAuditEntry();
                service.Write(first);
                service.Write(second);

                // Background drain is asynchronous; poll (up to ~5s) until both entries land.
                for (int i = 0; i < 250 && sink.Count < 2; i++)
                {
                    await Task.Delay(20);
                }

                var written = sink.Snapshot();
                Assert.Contains(first, written);
                Assert.Contains(second, written);
            }
            finally
            {
                await service.StopAsync(CancellationToken.None);
            }
        }
    }
}
