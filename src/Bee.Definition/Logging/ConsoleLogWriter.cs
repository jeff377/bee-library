namespace Bee.Definition.Logging
{
    /// <summary>
    /// A log writer implementation that outputs log entries to the console.
    /// </summary>
    public class ConsoleLogWriter : ILogWriter
    {
        /// <summary>
        /// Writes a log entry to the console.
        /// </summary>
        /// <param name="entry">The log entry.</param>
        public void Write(LogEntry entry)
        {
            switch (entry.EntryType)
            {
                case LogEntryType.Information:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogEntryType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogEntryType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine($"[{entry.EntryType}] [{entry.Source}] {entry.Message}");

            if (entry.Exception != null)
            {
                Console.WriteLine(entry.Exception.ToString());
            }

            Console.ResetColor();
        }
    }
}
