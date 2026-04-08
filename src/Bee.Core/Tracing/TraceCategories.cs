namespace Bee.Core.Tracing
{
    /// <summary>
    /// Defines commonly used trace category constants for use with <see cref="TraceEvent.Category"/>.
    /// </summary>
    public static class TraceCategories
    {
        /// <summary>
        /// General purpose.
        /// </summary>
        public const string General = "General";
        /// <summary>
        /// JSON-RPC request or response model.
        /// </summary>
        public const string JsonRpc = "JsonRpc";
    }
}
