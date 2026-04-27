namespace Bee.Definition.Logging
{
    /// <summary>
    /// The type of a log entry event.
    /// </summary>
    public enum LogEntryType
    {
        /// <summary>
        /// Informational message indicating normal system operation.
        /// </summary>
        Information,
        /// <summary>
        /// Warning message indicating a possible abnormal condition while the system can still continue.
        /// </summary>
        Warning,
        /// <summary>
        /// Error message indicating an exception or failure during system execution.
        /// </summary>
        Error
    }
}
