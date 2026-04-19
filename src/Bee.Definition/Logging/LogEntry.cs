namespace Bee.Definition.Logging
{
    /// <summary>
    /// A log entry object for system log events, modeled after EventLogEntry.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the entry type (e.g., Information, Warning, or Error).
        /// </summary>
        public LogEntryType EntryType { get; set; }

        /// <summary>
        /// Gets or sets the source module or component name (e.g., "EmployeeBusinessObject").
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category code, which can be used to distinguish functional modules or subsystems.
        /// </summary>
        public short Category { get; set; }

        /// <summary>
        /// Gets or sets the host name or machine name where the application is running.
        /// </summary>
        public string MachineName { get; set; } = Environment.MachineName;

        /// <summary>
        /// Gets or sets the main message content.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp (defaults to the current time).
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the optional exception object.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
