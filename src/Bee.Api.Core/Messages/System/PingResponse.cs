using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the ping operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
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
        /// Gets or sets the server version.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the trace identifier.
        /// </summary>
        public string? TraceId { get; set; }
    }
}
