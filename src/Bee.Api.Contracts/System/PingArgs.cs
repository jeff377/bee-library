using System;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Input arguments for the Ping method.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class PingArgs : BusinessArgs
    {
        /// <summary>
        /// Gets or sets the client identifier name (optional).
        /// </summary>
        [Key(100)]
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the call trace ID (optional).
        /// </summary>
        [Key(101)]
        public string TraceId { get; set; }
    }
}
