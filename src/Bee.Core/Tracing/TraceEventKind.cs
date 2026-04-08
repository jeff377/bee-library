namespace Bee.Core.Tracing
{
    /// <summary>
    /// The kind of trace event, used to distinguish start, end, and point events.
    /// </summary>
    public enum TraceEventKind
    {
        /// <summary>
        /// Represents the start event of a trace segment;
        /// typically produced by <see cref="ITraceListener.TraceStart"/>.
        /// </summary>
        Start = 0,
        /// <summary>
        /// Represents the end event of a trace segment;
        /// typically produced by <see cref="ITraceListener.TraceEnd"/>,
        /// and includes the elapsed time and execution status of the segment.
        /// </summary>
        End = 1,
        /// <summary>
        /// Represents a single-point event that does not require a paired start/end call;
        /// typically produced by <see cref="ITraceListener.TraceWrite"/>.
        /// </summary>
        Point = 2
    }
}
