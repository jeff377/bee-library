using System.ComponentModel;
using Bee.Db.Logging;

namespace Bee.Db.UnitTests
{
    public class DbAccessLoggerTests
    {
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
            // BackendInfo.LogOptions 在無設定 fixture 時通常為 null，LogEnd 應安全 return
            var command = new Bee.Db.DbCommandSpec(
                Bee.Db.DbCommandKind.Scalar, "SELECT 1");
            var context = DbAccessLogger.LogStart(command);

            var ex = Record.Exception(() => DbAccessLogger.LogEnd(context, affectedRows: 0));

            Assert.Null(ex);
            Assert.False(context.Stopwatch.IsRunning);
        }
    }
}
