using System.Runtime.CompilerServices;

namespace Bee.Base.Tracing
{
    /// <summary>
    /// Defines the interface for execution flow monitoring, providing methods to start and end trace segments.
    /// Called by the application at each layer to create a <see cref="TraceContext"/> and emit trace events.
    /// </summary>
    public interface ITraceListener
    {
        /// <summary>
        /// Starts a trace segment and returns the corresponding <see cref="TraceContext"/>.
        /// </summary>
        /// <param name="layer">The trace layer this segment belongs to.</param>
        /// <param name="detail">Additional description, e.g. a SQL statement or API route.</param>
        /// <param name="name">The monitor name, e.g. a method or event name; automatically populated with the caller's method name if not set.</param>
        /// <param name="category">The trace category, used by the Trace Viewer to parse the Tag by category.</param>
        /// <param name="tag">The trace object; content is interpreted based on Category.</param>
        TraceContext TraceStart(
            TraceLayer layer, string detail = "", [CallerMemberName] string name = "",
            string category = "", object tag = null);

        /// <summary>
        /// Ends the specified trace segment and emits the corresponding <see cref="TraceEvent"/>.
        /// </summary>
        /// <param name="ctx">The context created when the trace was started.</param>
        /// <param name="status">The execution status, e.g. Ok, Error, or Cancelled.</param>
        /// <param name="detail">Additional description; overrides the Detail set at start if provided.</param>
        void TraceEnd(TraceContext ctx, TraceStatus status = TraceStatus.Ok, string detail = null);

        /// <summary>
        /// Writes a single-point trace event at any position without requiring a paired start/end call.
        /// </summary>
        /// <param name="layer">The layer this event belongs to.</param>
        /// <param name="detail">The event description.</param>
        /// <param name="name">The monitor name, e.g. a method or event name; automatically populated with the caller's method name if not set.</param>
        /// <param name="status">The execution status.</param>
        /// <param name="category">The trace category, used by the Trace Viewer to parse the Tag by category.</param>
        /// <param name="tag">The trace object; content is interpreted based on Category.</param>
        void TraceWrite(
            TraceLayer layer, string detail = "", [CallerMemberName] string name = "", TraceStatus status = TraceStatus.Ok,
            string category = "", object tag = null);
    }
}
