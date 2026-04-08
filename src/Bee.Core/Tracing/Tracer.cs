using System.Runtime.CompilerServices;

namespace Bee.Core.Tracing
{
    /// <summary>
    /// Static tracer utility class.
    /// </summary>
    public static class Tracer
    {
        /// <summary>
        /// Gets a value indicating whether tracing is enabled.
        /// </summary>
        public static bool Enabled
            => SysInfo.TraceEnabled && SysInfo.TraceListener != null;

        /// <summary>
        /// Starts a trace segment.
        /// </summary>
        /// <param name="layer">The layer this segment belongs to, e.g. UI, API, Biz, or Data.</param>
        /// <param name="name">The monitor name, e.g. a method name or event name.</param>
        /// <param name="detail">Additional description, e.g. a SQL statement or API route.</param>
        /// <param name="category">The trace category, used by the Trace Viewer to parse the Tag by category.</param>
        /// <param name="tag">The trace object; content is interpreted based on Category.</param>
        /// <returns>The created trace context object.</returns>
        public static TraceContext Start(
            TraceLayer layer, string detail = "", [CallerMemberName] string name = "",
            string category = "", object tag = null)
        {
            if (!Enabled) { return null; }
            return SysInfo.TraceListener.TraceStart(layer, detail, name, category, tag);
        }

        /// <summary>
        /// Ends a trace segment.
        /// </summary>
        /// <param name="ctx">The context created when the trace was started.</param>
        /// <param name="status">The execution status, e.g. Ok, Error, or Cancelled.</param>
        /// <param name="detail">Additional description; overrides the Detail set at start if provided.</param>
        public static void End(TraceContext ctx, TraceStatus status = TraceStatus.Ok, string detail = null)
        {
            if (!Enabled || ctx == null) return;
            SysInfo.TraceListener.TraceEnd(ctx, status, detail);
        }

        /// <summary>
        /// Writes a single-point trace event.
        /// </summary>
        /// <param name="layer">The layer this event belongs to.</param>
        /// <param name="detail">The event description.</param>
        /// <param name="name">The monitor name, e.g. a method or event name; automatically populated with the caller's method name if not set.</param>
        /// <param name="status">The execution status.</param>
        /// <param name="category">The trace category, used by the Trace Viewer to parse the Tag by category.</param>
        /// <param name="tag">The trace object; content is interpreted based on Category.</param>
        public static void Write(
            TraceLayer layer, string detail = "", [CallerMemberName] string name = "", TraceStatus status = TraceStatus.Ok,
            string category = "", object tag = null)
        {
            if (!Enabled) return;
            SysInfo.TraceListener.TraceWrite(layer, detail, name, status, category, tag);
        }
    }
}
