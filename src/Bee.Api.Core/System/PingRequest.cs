using System;
using Bee.Definition.Api;
using MessagePack;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API request for the ping operation.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class PingRequest : ApiRequest, IPingRequest
    {
        /// <summary>
        /// Gets or sets the client name.
        /// </summary>
        [Key(100)]
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the trace identifier.
        /// </summary>
        [Key(101)]
        public string TraceId { get; set; }
    }
}
