using System;
using System.Runtime.CompilerServices;

namespace Bee.Base.Tracing
{
    /// <summary>
    /// Default execution flow monitor.
    /// </summary>
    public sealed class TraceListener : ITraceListener
    {
        private readonly ITraceWriter _writer;

        /// <summary>
        /// Initializes a new instance of <see cref="TraceListener"/>.
        /// </summary>
        /// <param name="writer">The trace writer used for output.</param>
        public TraceListener(ITraceWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        /// <summary>
        /// Starts a trace segment and returns the corresponding <see cref="TraceContext"/>.
        /// </summary>
        /// <param name="layer">The trace layer this segment belongs to.</param>
        /// <param name="detail">Additional description, e.g. a SQL statement or API route.</param>
        /// <param name="name">The monitor name, e.g. a method or event name; automatically populated with the caller's method name if not set.</param>
        /// <param name="category">The trace category, used by the Trace Viewer to parse the Tag by category.</param>
        /// <param name="tag">The trace object; content is interpreted based on Category.</param>
        public TraceContext TraceStart(
            TraceLayers layer, string detail = "",
            string category = "", object? tag = null,
            [CallerMemberName] string name = "")
        {
            var ctx = new TraceContext(layer, name, detail, category, tag);

            _writer.Write(new TraceEvent
            {
                Time = ctx.Start,
                Layer = ctx.Layer,
                Name = ctx.Name,
                Detail = detail ?? ctx.Detail,
                Category = ctx.Category,
                Tag = ctx.Tag,
                Kind = TraceEventKind.Start
            });

            return ctx;
        }

        /// <summary>
        /// Ends the specified trace segment and emits the corresponding <see cref="TraceEvent"/>.
        /// </summary>
        /// <param name="ctx">The context created when the trace was started.</param>
        /// <param name="status">The execution status, e.g. Ok, Error, or Cancelled.</param>
        /// <param name="detail">Additional description; overrides the Detail set at start if provided.</param>
        public void TraceEnd(TraceContext ctx, TraceStatus status = TraceStatus.Ok, string? detail = null)
        {
            if (ctx == null) return;
            ctx.Stopwatch.Stop();

            _writer.Write(new TraceEvent
            {
                Time = ctx.Start,
                Layer = ctx.Layer,
                Name = ctx.Name,
                Detail = detail ?? ctx.Detail,
                DurationMs = ctx.Stopwatch.Elapsed.TotalMilliseconds,
                Category = ctx.Category,
                Tag = ctx.Tag,
                Kind = TraceEventKind.End,
                Status = status
            });
        }

        /// <summary>
        /// Writes a single-point trace event at any position without requiring a paired start/end call.
        /// </summary>
        /// <param name="layer">The layer this event belongs to.</param>
        /// <param name="detail">The event description.</param>
        /// <param name="name">The monitor name, e.g. a method or event name; automatically populated with the caller's method name if not set.</param>
        /// <param name="status">The execution status.</param>
        /// <param name="category">The trace category, used by the Trace Viewer to parse the Tag by category.</param>
        /// <param name="tag">The trace object; content is interpreted based on Category.</param>
        public void TraceWrite(
            TraceLayers layer, string detail = "", TraceStatus status = TraceStatus.Ok,
            string category = "", object? tag = null,
            [CallerMemberName] string name = "")
        {
            _writer.Write(new TraceEvent
            {
                Time = DateTimeOffset.Now,
                Layer = layer,
                Name = name,
                Detail = detail,
                DurationMs = 0,
                Category = category,
                Tag = tag,
                Kind = TraceEventKind.Point,
                Status = status
            });
        }
    }

}
