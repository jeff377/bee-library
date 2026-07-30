using Bee.Api.Contracts.System;
using Bee.Definition.Security;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for the Ping method.
    /// </summary>
    public class PingResult : BusinessResult, IPingResponse
    {
        /// <summary>
        /// Gets or sets the status, typically "ok" or "pong".
        /// </summary>
        public string Status { get; set; } = "ok";

        /// <summary>
        /// Gets or sets the current server UTC time.
        /// </summary>
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the outcome of the API key check for this call.
        /// </summary>
        public ApiKeyStatus ApiKeyStatus { get; set; }

        /// <summary>
        /// Gets or sets the optional version information; <c>null</c> when the caller did not present
        /// an accepted API key.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the echoed trace ID (if provided).
        /// </summary>
        public string? TraceId { get; set; }
    }
}
