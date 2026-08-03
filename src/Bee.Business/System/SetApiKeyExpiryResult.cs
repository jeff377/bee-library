using Bee.Api.Contracts.System;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for setting or clearing an issued API key's expiry.
    /// </summary>
    public class SetApiKeyExpiryResult : BusinessResult, ISetApiKeyExpiryResponse
    {
        /// <summary>
        /// Gets or sets the key identifier whose expiry was set.
        /// </summary>
        public string SysId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expiry now stored for the key, or <c>null</c> when it does not expire.
        /// </summary>
        public DateTime? ExpiredAt { get; set; }
    }
}
