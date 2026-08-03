namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the set API key expiry response.
    /// </summary>
    public interface ISetApiKeyExpiryResponse
    {
        /// <summary>
        /// Gets the key identifier whose expiry was set.
        /// </summary>
        string SysId { get; }

        /// <summary>
        /// Gets the expiry now stored for the key, or <c>null</c> when it does not expire.
        /// </summary>
        DateTime? ExpiredAt { get; }
    }
}
