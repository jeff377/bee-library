namespace Bee.Base.Serialization
{
    /// <summary>
    /// Keys used on <see cref="System.Exception.Data"/> by the file-backed codec entry points.
    /// </summary>
    /// <remarks>
    /// These carry the detail that must **not** appear in the exception message. A deserialization
    /// failure surfaces as <see cref="System.InvalidOperationException"/>, which the JSON-RPC error
    /// contract maps to a code whose handling returns the message to the caller verbatim — so the
    /// message names the file, and the path that would disclose the server's directory layout
    /// travels here instead, where only server-side logging sees it.
    /// </remarks>
    public static class SerializationErrorData
    {
        /// <summary>Key holding the full path of the file being read when the failure occurred.</summary>
        public const string FilePath = "Bee.FilePath";
    }
}
