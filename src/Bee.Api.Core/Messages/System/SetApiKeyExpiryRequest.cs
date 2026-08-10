using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the set API key expiry operation.
    /// </summary>
    public class SetApiKeyExpiryRequest : ApiRequest, ISetApiKeyExpiryRequest
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
