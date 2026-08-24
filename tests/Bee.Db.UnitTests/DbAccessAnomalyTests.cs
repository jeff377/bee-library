using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Logging;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 驗證 <c>DbAccess</c> 的異常偵測（<c>DbAccess.Anomaly.cs</c>）：門檻判定、Kind 分類、
    /// 以及「異常寫入為 best-effort，不得改變命令結果」這條約束。
    /// </summary>
    /// <remarks>
    /// 以 SQLite 執行真實命令而非 mock <c>DbAccess</c>：門檻判定吃的是實際的 RowsAffected /
    /// Rows.Count / 例外型別，換成假的執行結果就繞過了被測邏輯本身。
    /// </remarks>
    public class DbAccessAnomalyTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public DbAccessAnomalyTests(SharedDbFixture fx) { _fx = fx; }

        /// <summary>
        /// 收集 <see cref="DbAnomalyEntry"/> 的測試用 writer。
        /// </summary>
        private sealed class CapturingAnomalyLogWriter : IAnomalyLogWriter
        {
            public List<DbAnomalyEntry> Entries { get; } = [];

            public void Write(AnomalyEntry entry)
            {
                if (entry is DbAnomalyEntry anomaly) { Entries.Add(anomaly); }
            }
        }

        private DbAccess NewSqliteDbAccess(
            IAnomalyLogWriter? writer, DbAccessAnomalyLogOptions? options, int maxCommandTimeout = 0)
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.SQLite);
            return new DbAccess(databaseId, _fx.GetRequiredService<IDbConnectionManager>(),
                maxCommandTimeout, writer, options);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("未設定 writer 時命令照常執行且不寫任何異常記錄")]
        public void NoWriter_ExecutesWithoutAnomaly()
        {
            var writer = new CapturingAnomalyLogWriter();
            var dbAccess = NewSqliteDbAccess(writer: null, options: null);

            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, "SELECT 1"));

            Assert.Equal(1L, Convert.ToInt64(result.Scalar, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Empty(writer.Entries);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("Level 為 None 時即使門檻全開也不寫異常記錄")]
        public void LevelNone_WritesNothing()
        {
            var writer = new CapturingAnomalyLogWriter();
            var options = new DbAccessAnomalyLogOptions
            {
                Level = DbAccessAnomalyLogLevel.None,
                ExecutionTimeThreshold = 1,
                ResultRowThreshold = 1
            };
            var dbAccess = NewSqliteDbAccess(writer, options);

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, "SELECT 1 UNION ALL SELECT 2"));

            Assert.Empty(writer.Entries);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("Level 為 Error 時成功命令不記錄 Slow / LargeResult")]
        public void LevelError_SkipsSuccessAnomalies()
        {
            var writer = new CapturingAnomalyLogWriter();
            var options = new DbAccessAnomalyLogOptions
            {
                Level = DbAccessAnomalyLogLevel.Error,
                ResultRowThreshold = 1
            };
            var dbAccess = NewSqliteDbAccess(writer, options);

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, "SELECT 1 UNION ALL SELECT 2"));

            Assert.Empty(writer.Entries);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("結果列數超過門檻應記錄 LargeResult 並帶回實際列數")]
        public void ResultRowThresholdExceeded_LogsLargeResult()
        {
            var writer = new CapturingAnomalyLogWriter();
            var options = new DbAccessAnomalyLogOptions
            {
                Level = DbAccessAnomalyLogLevel.Warning,
                ResultRowThreshold = 1,
                AffectedRowThreshold = 0,
                ExecutionTimeThreshold = 0
            };
            var dbAccess = NewSqliteDbAccess(writer, options);

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, "SELECT 1 UNION ALL SELECT 2"));

            var entry = Assert.Single(writer.Entries);
            Assert.Equal(AnomalyKind.LargeResult, entry.Kind);
            Assert.Equal(2, entry.ResultRows);
            Assert.Equal(TestDbConventions.GetDatabaseId(DatabaseType.SQLite), entry.DatabaseId);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("影響列數超過門檻應記錄 LargeAffected")]
        public void AffectedRowThresholdExceeded_LogsLargeAffected()
        {
            var writer = new CapturingAnomalyLogWriter();
            var options = new DbAccessAnomalyLogOptions
            {
                Level = DbAccessAnomalyLogLevel.Warning,
                AffectedRowThreshold = 1,
                ResultRowThreshold = 0,
                ExecutionTimeThreshold = 0
            };
            var dbAccess = NewSqliteDbAccess(writer, options);
            var table = $"t_anomaly_affected_{Guid.NewGuid():N}";

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"CREATE TABLE {table} (id INTEGER)"));
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {table} (id) SELECT 1 UNION ALL SELECT 2"));

            var entry = Assert.Single(writer.Entries);
            Assert.Equal(AnomalyKind.LargeAffected, entry.Kind);
            Assert.Equal(2, entry.AffectedRows);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("執行時間門檻為 0 時不記錄 Slow")]
        public void ExecutionTimeThresholdDisabled_SkipsSlow()
        {
            var writer = new CapturingAnomalyLogWriter();
            var options = new DbAccessAnomalyLogOptions
            {
                Level = DbAccessAnomalyLogLevel.Warning,
                ExecutionTimeThreshold = 0,
                AffectedRowThreshold = 0,
                ResultRowThreshold = 0
            };
            var dbAccess = NewSqliteDbAccess(writer, options);

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, "SELECT 1"));

            Assert.Empty(writer.Entries);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("命令失敗應記錄 Error 並保留原例外，命令文字不含參數值")]
        public void CommandFails_LogsErrorAndRethrows()
        {
            var writer = new CapturingAnomalyLogWriter();
            var options = new DbAccessAnomalyLogOptions { Level = DbAccessAnomalyLogLevel.Error };
            var dbAccess = NewSqliteDbAccess(writer, options);
            var commandText = "SELECT * FROM __no_such_table__ WHERE id = {0}";

            var exception = Record.Exception(() =>
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, commandText, 123)));

            Assert.NotNull(exception);
            var entry = Assert.Single(writer.Entries);
            Assert.Equal(AnomalyKind.Error, entry.Kind);
            Assert.Equal(commandText, entry.Command);
            Assert.DoesNotContain("123", entry.Command, StringComparison.Ordinal);
            Assert.False(string.IsNullOrEmpty(entry.ErrorType));
            Assert.False(string.IsNullOrEmpty(entry.ErrorMessage));
            // 訊息被攤平成單行後才落地，避免 log 欄位夾帶換行。
            Assert.DoesNotContain('\n', entry.ErrorMessage!);
            Assert.DoesNotContain('\r', entry.ErrorMessage!);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("例外訊息含 timeout 字樣應分類為 Timeout")]
        public void TimeoutWorded_ClassifiesAsTimeout()
        {
            var writer = new CapturingAnomalyLogWriter();
            var options = new DbAccessAnomalyLogOptions { Level = DbAccessAnomalyLogLevel.Error };
            var dbAccess = NewSqliteDbAccess(writer, options);

            // SQLite 對未知函式的錯誤訊息會帶回函式名，藉此讓 `IsTimeout` 的訊息比對命中，
            // 而不必真的讓命令逾時（逾時測試在 CI 上必然是不穩定來源）。
            var exception = Record.Exception(() =>
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, "SELECT timeout(1)")));

            Assert.NotNull(exception);
            var entry = Assert.Single(writer.Entries);
            Assert.Equal(AnomalyKind.Timeout, entry.Kind);
        }
    }
}
