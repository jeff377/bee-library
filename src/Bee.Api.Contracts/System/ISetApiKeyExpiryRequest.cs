namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the set API key expiry request.
    /// </summary>
    public interface ISetApiKeyExpiryRequest
    {
        /// <summary>
        /// Gets the key identifier (<c>st_api_key.sys_id</c>) whose expiry is being set.
        /// </summary>
        string SysId { get; }

        /// <summary>
        /// Gets the UTC expiry, or <c>null</c> to clear it so the key does not expire.
        /// </summary>
        DateTime? ExpiredAt { get; }
    }
}
