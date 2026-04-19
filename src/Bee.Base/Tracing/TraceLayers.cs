namespace Bee.Base.Tracing
{
    /// <summary>
    /// The layer a trace event belongs to, used to distinguish different execution positions in the system,
    /// such as UI, API client, API server, business layer, or data access layer.
    /// </summary>
    [Flags]
    public enum TraceLayers
    {
        /// <summary>
        /// No layer (default value).
        /// </summary>
        None = 0,
        /// <summary>
        /// User interface layer, e.g. WinForms, Blazor, or MAUI interactions.
        /// </summary>
        UI = 1 << 0,
        /// <summary>
        /// API client layer, e.g. frontend or external system calling an API.
        /// </summary>
        ApiClient = 1 << 1,
        /// <summary>
        /// API server layer, e.g. backend API Controller or Middleware.
        /// </summary>
        ApiServer = 1 << 2,
        /// <summary>
        /// Business layer, e.g. execution of a Service or Domain Service.
        /// </summary>
        Business = 1 << 3,
        /// <summary>
        /// Data access layer, e.g. SQL execution via EF Core, Dapper, or ADO.NET.
        /// </summary>
        Data = 1 << 4,
        /// <summary>
        /// All layers (includes UI, API client, API server, business, and data access).
        /// </summary>
        All = UI | ApiClient | ApiServer | Business | Data
    }
}
