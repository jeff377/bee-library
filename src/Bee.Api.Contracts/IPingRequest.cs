namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for ping request parameters.
    /// </summary>
    public interface IPingRequest
    {
        /// <summary>
        /// Gets the client name.
        /// </summary>
        string? ClientName { get; }

        /// <summary>
        /// Gets the trace identifier.
        /// </summary>
        string? TraceId { get; }
    }
}
