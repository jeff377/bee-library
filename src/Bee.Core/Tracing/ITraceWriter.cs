namespace Bee.Core.Tracing
{
    /// <summary>
    /// Interface for writing trace output, responsible for sending <see cref="TraceEvent"/> instances to various destinations
    /// such as a WinForms UI, file, console, or external monitoring system.
    /// </summary>
    public interface ITraceWriter
    {
        /// <summary>
        /// Writes the specified trace event to the destination.
        /// </summary>
        /// <param name="evt">The trace event to write.</param>
        void Write(TraceEvent evt);
    }

}
