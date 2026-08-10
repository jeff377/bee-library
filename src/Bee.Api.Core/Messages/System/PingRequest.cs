using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the ping operation.
    /// </summary>
    public class PingRequest : ApiRequest, IPingRequest
    {
        /// <summary>
        /// Gets or sets the client name.
        /// </summary>
        public string? ClientName { get; set; }

        /// <summary>
        /// Gets or sets the trace identifier.
        /// </summary>
        public string? TraceId { get; set; }
    }
}
