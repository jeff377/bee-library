using Bee.Api.Contracts.System;
using Bee.Definition.Security;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the ping operation.
    /// </summary>
    public class PingResponse : ApiResponse, IPingResponse
    {
        /// <summary>
        /// Gets or sets the server status.
        /// </summary>
        public string Status { get; set; } = "ok";

        /// <summary>
        /// Gets or sets the server time in UTC.
        /// </summary>
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the outcome of the API key check for this call.
        /// </summary>
        public ApiKeyStatus ApiKeyStatus { get; set; }

        /// <summary>
        /// Gets or sets the server version; <c>null</c> when the caller did not present an accepted
        /// API key.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the trace identifier.
        /// </summary>
        public string? TraceId { get; set; }
    }
}
