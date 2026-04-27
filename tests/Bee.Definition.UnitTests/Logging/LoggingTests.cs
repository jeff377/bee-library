using System.ComponentModel;
using Bee.Definition.Logging;
using Bee.Definition.Database;

namespace Bee.Definition.UnitTests.Logging
{
    /// <summary>
    /// Logging 模組基本行為測試：LogEntry、LogOptions、ConsoleLogWriter、NullLogWriter。
    /// </summary>
    public class LoggingTests
    {
        [Fact]
        [DisplayName("LogEntry 預設值應包含本機名稱與當前時間")]
        public void LogEntry_Defaults_PopulatesMachineAndTimestamp()
        {
            // Act
            var entry = new LogEntry();

            // Assert
            Assert.Equal(Environment.MachineName, entry.MachineName);
            Assert.NotEqual(default(DateTime), entry.Timestamp);
            Assert.Equal(string.Empty, entry.Source);
            Assert.Equal(string.Empty, entry.Message);
            Assert.Null(entry.Exception);
        }

        [Fact]
        [DisplayName("LogEntry ToString 應回傳類別名稱")]
        public void LogEntry_ToString_ReturnsTypeName()
        {
            // Arrange
            var entry = new LogEntry();

            // Act & Assert
            Assert.Equal(nameof(LogEntry), entry.ToString());
        }

        [Fact]
        [DisplayName("LogOptions 預設應包含非 null 的 DbAccess 選項")]
        public void LogOptions_Defaults_HasDbAccessOptions()
        {
            // Act
            var options = new LogOptions();

            // Assert
            Assert.NotNull(options.DbAccess);
            Assert.Equal(nameof(LogOptions), options.ToString());
        }

        [Fact]
        [DisplayName("DbAccessAnomalyLogOptions 預設值應符合規格")]
        public void DbAccessAnomalyLogOptions_Defaults_MatchExpected()
        {
            // Act
            var options = new DbAccessAnomalyLogOptions();

            // Assert
            Assert.Equal(DbAccessAnomalyLogLevel.Warning, options.Level);
            Assert.Equal(10000, options.AffectedRowThreshold);
            Assert.Equal(10000, options.ResultRowThreshold);
            Assert.Equal(300, options.ExecutionTimeThreshold);
            Assert.Equal(nameof(DbAccessAnomalyLogOptions), options.ToString());
        }

        [Theory]
        [InlineData(LogEntryType.Information)]
        [InlineData(LogEntryType.Warning)]
        [InlineData(LogEntryType.Error)]
        [DisplayName("ConsoleLogWriter 寫入各類型應輸出到 Console 且不拋出例外")]
        public void ConsoleLogWriter_Write_ProducesConsoleOutput(LogEntryType type)
        {
            // Arrange
            var writer = new ConsoleLogWriter();
            var entry = new LogEntry
            {
                EntryType = type,
                Source = "Test",
                Message = "Hello"
            };

            var originalOut = Console.Out;
            var sb = new StringWriter();
            Console.SetOut(sb);
            try
            {
                // Act
                writer.Write(entry);

                // Assert
                var output = sb.ToString();
                Assert.Contains("Test", output);
                Assert.Contains("Hello", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        [DisplayName("ConsoleLogWriter 附帶 Exception 應輸出例外資訊")]
        public void ConsoleLogWriter_Write_WithException_OutputsExceptionText()
        {
            // Arrange
            var writer = new ConsoleLogWriter();
            var entry = new LogEntry
            {
                EntryType = LogEntryType.Error,
                Source = "Svc",
                Message = "Boom",
                Exception = new InvalidOperationException("Oops")
            };

            var originalOut = Console.Out;
            var sb = new StringWriter();
            Console.SetOut(sb);
            try
            {
                // Act
                writer.Write(entry);

                // Assert
                Assert.Contains("Oops", sb.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        [DisplayName("NullLogWriter 應不產生任何輸出且不拋出例外")]
        public void NullLogWriter_Write_NoOp()
        {
            // Arrange
            var writer = new NullLogWriter();
            var entry = new LogEntry
            {
                EntryType = LogEntryType.Information,
                Source = "Any",
                Message = "ShouldNotAppear"
            };

            var originalOut = Console.Out;
            var sb = new StringWriter();
            Console.SetOut(sb);
            try
            {
                // Act
                writer.Write(entry);

                // Assert
                Assert.Equal(string.Empty, sb.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}
