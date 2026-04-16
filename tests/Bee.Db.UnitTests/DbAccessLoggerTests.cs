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
    }
}
