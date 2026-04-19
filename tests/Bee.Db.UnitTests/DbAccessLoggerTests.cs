using System.ComponentModel;
using Bee.Db.Logging;
using Bee.Definition;
using Bee.Definition.Logging;

namespace Bee.Db.UnitTests
{
    public class DbAccessLoggerTests
    {
        private static DbCommandSpec BuildCommand(string text = "SELECT 1")
            => new DbCommandSpec(DbCommandKind.Scalar, text);

        /// <summary>
        /// 暫時替換 <see cref="BackendInfo.LogOptions"/>，用後還原。
        /// </summary>
        private static DisposableAction UseLogOptions(LogOptions? replacement)
        {
            var original = BackendInfo.LogOptions;
            BackendInfo.LogOptions = replacement!;
            return new DisposableAction(() => BackendInfo.LogOptions = original);
        }

        private sealed class DisposableAction : IDisposable
        {
            private readonly Action _dispose;
            public DisposableAction(Action dispose) => _dispose = dispose;
            public void Dispose() => _dispose();
        }

        [Fact]
        [DisplayName("LogError 不應擲出例外")]
        public void LogError_ValidContext_DoesNotThrow()
        {
            var command = new Bee.Db.DbCommandSpec(
                Bee.Db.DbCommandKind.NonQuery,
                "UPDATE test SET col={0}", "value");
            var context = DbAccessLogger.LogStart(command, "testDb");

            var ex = new InvalidOperationException("Test error");
            var exception = Record.Exception(() => DbAccessLogger.LogError(context, ex));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("LogError context 為 null 應擲出 ArgumentNullException")]
        public void LogError_NullContext_ThrowsArgumentNull()
        {
            var ex = new InvalidOperationException("Test");
            Assert.Throws<ArgumentNullException>(() => DbAccessLogger.LogError(null!, ex));
        }

        [Fact]
        [DisplayName("LogError exception 為 null 應擲出 ArgumentNullException")]
        public void LogError_NullException_ThrowsArgumentNull()
        {
            var command = new Bee.Db.DbCommandSpec(
                Bee.Db.DbCommandKind.NonQuery, "SELECT 1");
            var context = DbAccessLogger.LogStart(command);
            Assert.Throws<ArgumentNullException>(() => DbAccessLogger.LogError(context, null!));
        }

        [Fact]
        [DisplayName("LogStart 應回傳含 CommandText 與 DatabaseId 的 context 並啟動 Stopwatch")]
        public void LogStart_ReturnsContextWithRunningStopwatch()
        {
            var command = new Bee.Db.DbCommandSpec(
                Bee.Db.DbCommandKind.Scalar, "SELECT 1");

            var context = DbAccessLogger.LogStart(command, "common");

            Assert.NotNull(context);
            Assert.Equal("SELECT 1", context.CommandText);
            Assert.Equal("common", context.DatabaseId);
            Assert.True(context.Stopwatch.IsRunning);
        }

        [Fact]
        [DisplayName("LogEnd context 為 null 應擲出 ArgumentNullException")]
        public void LogEnd_NullContext_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => DbAccessLogger.LogEnd(null!));
        }

        [Fact]
        [DisplayName("LogEnd 應停止 Stopwatch 且 LogOptions 為 null 不應擲例外")]
        public void LogEnd_StopsStopwatchAndDoesNotThrowWhenLogOptionsNull()
        {
            using var _ = UseLogOptions(null);

            var context = DbAccessLogger.LogStart(BuildCommand());

            var ex = Record.Exception(() => DbAccessLogger.LogEnd(context, affectedRows: 0));

            Assert.Null(ex);
            Assert.False(context.Stopwatch.IsRunning);
        }

        [Fact]
        [DisplayName("LogEnd affectedRows 超過 threshold 應觸發 Warning 分支（不擲例外）")]
        public void LogEnd_AffectedRowsAboveThreshold_InvokesWarning()
        {
            var opts = new LogOptions
            {
                DbAccess = new DbAccessAnomalyLogOptions
                {
                    Level = DbAccessAnomalyLogLevel.Warning,
                    AffectedRowThreshold = 1,
                    ExecutionTimeThreshold = 0
                }
            };
            using var _ = UseLogOptions(opts);

            var context = DbAccessLogger.LogStart(BuildCommand());

            var ex = Record.Exception(() => DbAccessLogger.LogEnd(context, affectedRows: 10));

            Assert.Null(ex);
            Assert.False(context.Stopwatch.IsRunning);
        }

        [Theory]
        [InlineData(1.5, 0, true)]
        [InlineData(0.5, 0, false)]
        [DisplayName("ShouldWarn 於 Warning 等級且執行時間超過 threshold 應回傳 true")]
        public void ShouldWarn_ElapsedAboveThreshold_ReturnsExpected(double elapsedSeconds, int affectedRows, bool expected)
        {
            var opts = new DbAccessAnomalyLogOptions
            {
                Level = DbAccessAnomalyLogLevel.Warning,
                AffectedRowThreshold = 0,
                ExecutionTimeThreshold = 1
            };

            var result = DbAccessLogger.ShouldWarn(opts, elapsedSeconds, affectedRows);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("LogEnd Level=Error 即使 affectedRows 超過 threshold 也不應觸發 Warning")]
        public void LogEnd_LevelError_SkipsWarning()
        {
            var opts = new LogOptions
            {
                DbAccess = new DbAccessAnomalyLogOptions
                {
                    Level = DbAccessAnomalyLogLevel.Error,
                    AffectedRowThreshold = 1,
                    ExecutionTimeThreshold = 0
                }
            };
            using var _ = UseLogOptions(opts);

            var context = DbAccessLogger.LogStart(BuildCommand());

            var ex = Record.Exception(() => DbAccessLogger.LogEnd(context, affectedRows: 100));

            Assert.Null(ex);
            Assert.False(context.Stopwatch.IsRunning);
        }

        [Fact]
        [DisplayName("LogEnd DbAccess 選項為 null 應直接返回，不擲例外")]
        public void LogEnd_DbAccessOptionsNull_ReturnsEarly()
        {
            var opts = new LogOptions { DbAccess = null! };
            using var _ = UseLogOptions(opts);

            var context = DbAccessLogger.LogStart(BuildCommand());

            var ex = Record.Exception(() => DbAccessLogger.LogEnd(context, affectedRows: 999));

            Assert.Null(ex);
            Assert.False(context.Stopwatch.IsRunning);
        }
    }
}
