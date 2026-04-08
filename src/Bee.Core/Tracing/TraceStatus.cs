namespace Bee.Core.Tracing
{
    /// <summary>
    /// Execution status of a trace event, used to indicate the result of a trace.
    /// </summary>
    public enum TraceStatus
    {
        /// <summary>
        /// Completed successfully with no errors or interruptions.
        /// </summary>
        Ok,
        /// <summary>
        /// An error occurred, such as an exception or execution failure.
        /// </summary>
        Error,
        /// <summary>
        /// Execution was cancelled, e.g. by user interruption or system abort.
        /// </summary>
        Cancelled
    }
}
