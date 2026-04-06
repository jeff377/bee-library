using System;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Output result for the Ping method.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class PingResult : BusinessResult
    {
        /// <summary>
        /// Gets or sets the status, typically "ok" or "pong".
        /// </summary>
        [Key(100)]
        public string Status { get; set; } = "ok";

        /// <summary>
        /// Gets or sets the current server UTC time.
        /// </summary>
        [Key(101)]
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the optional version information.
        /// </summary>
        [Key(102)]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the echoed trace ID (if provided).
        /// </summary>
        [Key(103)]
        public string TraceId { get; set; }
    }
}
