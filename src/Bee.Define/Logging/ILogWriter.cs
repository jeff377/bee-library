namespace Bee.Define.Logging
{
    /// <summary>
    /// Interface for a system log writer.
    /// </summary>
    public interface ILogWriter
    {
        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <param name="entry">The log entry.</param>
        void Write(LogEntry entry);
    }
}
