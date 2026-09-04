using System.ComponentModel;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Hosting.Audit;
using Bee.Repository.Abstractions.AuditLog;
using Microsoft.Extensions.Logging.Abstractions;

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

        /// <summary>
        /// 每次寫入都擲例外的 sink —— 模擬部署自訂的 <see cref="IAuditLogSink"/> 失敗。
        /// </summary>
        private sealed class ThrowingAuditLogSink : IAuditLogSink
        {
            public int Attempts { get; private set; }

            public void WriteBatch(IReadOnlyList<AuditEntry> entries)
            {
                Attempts++;
                // 刻意用一個框架列舉不到的型別：sink 是公開的 DI 接縫，
                // 窄化的 catch 清單守不住它。
                throw new NotImplementedException("sink is broken");
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
            using var service = new AuditLogWriterService(sink, options, NullLogger<AuditLogWriterService>.Instance);

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
            using var service = new AuditLogWriterService(sink, options, NullLogger<AuditLogWriterService>.Instance);

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
    
        [Fact]
        [DisplayName("sink 擲例外時背景服務不得 fault（逸出會讓 .NET 預設行為停掉整個 host）")]
        public async Task ExecuteAsync_SinkThrows_ServiceKeepsRunning()
        {
            var sink = new ThrowingAuditLogSink();
            using var service = new AuditLogWriterService(
                sink, new AuditLogOptions(), NullLogger<AuditLogWriterService>.Instance);

            await service.StartAsync(CancellationToken.None);
            service.Write(new TestAuditEntry());

            // 等到 sink 真的被呼叫過，再確認服務仍然活著。
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (sink.Attempts == 0 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(10);
            }
            Assert.True(sink.Attempts > 0, "sink 從未被呼叫，這個測試沒有驗到東西。");

            // ExecuteTask 進入 Faulted 就是 BackgroundService 會讓 host 停掉的訊號。
            Assert.NotNull(service.ExecuteTask);
            Assert.NotEqual(TaskStatus.Faulted, service.ExecuteTask!.Status);

            // 後續項目仍會被嘗試寫入：迴圈沒有死。
            int before = sink.Attempts;
            service.Write(new TestAuditEntry());
            deadline = DateTime.UtcNow.AddSeconds(5);
            while (sink.Attempts == before && DateTime.UtcNow < deadline)
            {
                await Task.Delay(10);
            }
            Assert.True(sink.Attempts > before, "第一次失敗之後迴圈就停了。");

            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        [DisplayName("多執行緒同時寫入檔案 fallback 不得遺失批次")]
        public async Task SpillToFile_ConcurrentWriters_LoseNothing()
        {
            // 這條路徑只在 log 資料庫已經失敗時才會走到 —— 而那正是佇列塞滿、每條請求執行緒
            // 都湧進來的時刻。並行度的高峰與唯一會到達這段程式碼的時機是同一個。
            string path = Path.Combine(Path.GetTempPath(), $"bee_spill_{Guid.NewGuid():N}.log");
            var sink = new AuditLogDbSink(
                new AlwaysFailingWriteRepository(),
                new AuditLogOptions { FileFallbackPath = path },
                NullLogger<AuditLogDbSink>.Instance);

            const int Writers = 8;
            const int PerWriter = 25;
            try
            {
                await Task.WhenAll(Enumerable.Range(0, Writers).Select(_ => Task.Run(() =>
                {
                    for (int i = 0; i < PerWriter; i++)
                    {
                        sink.WriteBatch([new TestAuditEntry()]);
                    }
                })));

                var lines = File.ReadAllLines(path);
                Assert.Equal(Writers * PerWriter, lines.Length);
            }
            finally
            {
                if (File.Exists(path)) { File.Delete(path); }
            }
        }

        /// <summary>寫入一律失敗，強迫 <c>AuditLogDbSink</c> 走檔案 fallback。</summary>
        private sealed class AlwaysFailingWriteRepository : IAuditLogWriteRepository
        {
            public void WriteBatch(IReadOnlyList<AuditEntry> entries)
                => throw new InvalidOperationException("log database is down");
        }
}
}
