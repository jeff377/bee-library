using Bee.Api.Contracts;

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
        /// Gets or sets the optional version information.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the echoed trace ID (if provided).
        /// </summary>
        public string? TraceId { get; set; }
    }
}
