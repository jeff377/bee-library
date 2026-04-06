namespace Bee.Define.Logging
{
    /// <summary>
    /// A no-op log writer that performs no operations.
    /// Used as a default to avoid null checks.
    /// </summary>
    public class NullLogWriter : ILogWriter
    {
        /// <summary>
        /// Writes a log entry (this implementation does nothing).
        /// </summary>
        /// <param name="entry">The log entry.</param>
        public void Write(LogEntry entry)
        {
            // Do nothing.
        }
    }
}

