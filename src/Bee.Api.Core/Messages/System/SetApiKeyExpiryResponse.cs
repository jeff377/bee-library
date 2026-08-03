using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the set API key expiry operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class SetApiKeyExpiryResponse : ApiResponse, ISetApiKeyExpiryResponse
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
