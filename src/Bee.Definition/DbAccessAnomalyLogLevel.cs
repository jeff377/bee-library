namespace Bee.Definition
{
    /// <summary>
    /// Logging level for abnormal SQL executions.
    /// </summary>
    public enum DbAccessAnomalyLogLevel
    {
        /// <summary>
        /// Disable abnormal logging completely.
        /// </summary>
        None = 0,
        /// <summary>
        /// Log only errors and exceptions.
        /// </summary>
        Error = 1,
        /// <summary>
        /// Log errors, exceptions, and abnormal cases (slow queries, large updates, large result sets).
        /// </summary>
        Warning = 2
    }
}
