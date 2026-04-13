using Bee.Api.Contracts;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for the Ping method.
    /// </summary>
    public class PingArgs : BusinessArgs, IPingRequest
    {
        /// <summary>
        /// Gets or sets the client identifier name (optional).
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the call trace ID (optional).
        /// </summary>
        public string TraceId { get; set; }
    }
}
