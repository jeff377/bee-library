using Bee.Definition.Security;

namespace Bee.Api.Contracts.System
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
        /// Gets the outcome of the API key check for this call.
        /// </summary>
        /// <remarks>
        /// Reported because "test connection" is the main consumer of ping: a client that sends the
        /// wrong key would otherwise be told the connection is fine and only discover the problem on
        /// its first real call, in a different screen.
        /// </remarks>
        ApiKeyStatus ApiKeyStatus { get; }

        /// <summary>
        /// Gets the server version, or <c>null</c> when the caller did not present an accepted API
        /// key.
        /// </summary>
        /// <remarks>
        /// Ping does not require a key so that health checks keep working while the database is down,
        /// which would otherwise publish the framework version to anyone — the usual first step in
        /// fingerprinting. Status and server time are enough for a connectivity check; a monitor that
        /// wants the version can carry a key. Deployments that have not issued any key are unchanged:
        /// their gate is not in force, so the version is still reported.
        /// </remarks>
        string? Version { get; }

        /// <summary>
        /// Gets the trace identifier.
        /// </summary>
        string? TraceId { get; }
    }
}
