using System;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API response for the ping operation.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class PingResponse : ApiResponse, IPingResponse
    {
        /// <summary>
        /// Gets or sets the server status.
        /// </summary>
        [Key(100)]
        public string Status { get; set; } = "ok";

        /// <summary>
        /// Gets or sets the server time in UTC.
        /// </summary>
        [Key(101)]
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the server version.
        /// </summary>
        [Key(102)]
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the trace identifier.
        /// </summary>
        [Key(103)]
        public string? TraceId { get; set; }
    }
}
