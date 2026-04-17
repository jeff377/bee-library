using System;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for ping response data.
    /// </summary>
    public interface IPingResponse
    {
        /// <summary>
        /// Gets the server status.
        /// </summary>
        string Status { get; }

        /// <summary>
        /// Gets the server time in UTC.
        /// </summary>
        DateTime ServerTime { get; }

        /// <summary>
        /// Gets the server version.
        /// </summary>
        string? Version { get; }

        /// <summary>
        /// Gets the trace identifier.
        /// </summary>
        string? TraceId { get; }
    }
}
