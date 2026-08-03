using Bee.Api.Contracts.System;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for setting or clearing an issued API key's expiry.
    /// </summary>
    public class SetApiKeyExpiryArgs : BusinessArgs, ISetApiKeyExpiryRequest
    {
        /// <summary>
        /// Gets or sets the key identifier whose expiry is being set.
        /// </summary>
        public string SysId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC expiry, or <c>null</c> to clear it.
        /// </summary>
        public DateTime? ExpiredAt { get; set; }
    }
}
