using System;

namespace Bee.Base.Tracing
{
    /// <summary>
    /// Represents a complete trace event including start time, elapsed time, status, and description.
    /// </summary>
    public sealed class TraceEvent
    {
        /// <summary>
        /// Gets or sets the time the event occurred (for TraceEnd events, this is the start time).
        /// </summary>
        public DateTimeOffset Time { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Gets or sets the layer this event belongs to, e.g. UI, API, Biz, or Data.
        /// </summary>
        public TraceLayer Layer { get; set; }

        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event description, e.g. a SQL statement, API route, or business action summary.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the elapsed execution time in milliseconds. Zero for single-point TraceWrite events.
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the trace category, used by the Trace Viewer to parse the Tag by category.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the trace object; content is interpreted based on Category.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Gets or sets the kind of trace event.
        /// </summary>
        public TraceEventKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the execution status, e.g. Ok, Error, or Cancelled.
        /// </summary>
        public TraceStatus Status { get; set; } = TraceStatus.Ok;
    }

}
