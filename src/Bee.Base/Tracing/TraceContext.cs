using System;

namespace Bee.Base.Tracing
{
    /// <summary>
    /// Represents the execution context of a trace segment.
    /// Created by <see cref="ITraceListener.TraceStart"/> and consumed by <see cref="ITraceListener.TraceEnd"/>
    /// to calculate elapsed time and status.
    /// </summary>
    public sealed class TraceContext
    {
        /// <summary>
        /// Gets the layer this trace belongs to, e.g. UI, API, Biz, or Data.
        /// </summary>
        public TraceLayer Layer { get; }

        /// <summary>
        /// Gets the trace name, e.g. a method name or event name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets additional description, e.g. a SQL statement or API route.
        /// </summary>
        public string Detail { get; }

        /// <summary>
        /// Gets the trace start time (local time).
        /// </summary>
        public DateTimeOffset Start { get; }

        /// <summary>
        /// Gets the stopwatch used to measure elapsed execution time.
        /// </summary>
        public System.Diagnostics.Stopwatch Stopwatch { get; }

        /// <summary>
        /// Gets the trace category, used by the Trace Viewer to parse the Tag by category.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the trace object; content is interpreted based on Category.
        /// </summary>
        public object? Tag { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TraceContext"/>. Only <see cref="TraceListener"/> may create instances.
        /// </summary>
        internal TraceContext(TraceLayer layer, string name, string detail, string category = "", object? tag = null)
        {
            Layer = layer;
            Name = name ?? string.Empty;
            Detail = detail;
            Start = DateTimeOffset.Now; // Local time
            Stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Category = category ?? string.Empty;
            Tag = tag;
        }
    }


}
