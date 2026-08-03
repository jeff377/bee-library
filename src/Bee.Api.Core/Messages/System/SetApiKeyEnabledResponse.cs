using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the enable / disable API key operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class SetApiKeyEnabledResponse : ApiResponse, ISetApiKeyEnabledResponse
    {
        /// <summary>
        /// Gets or sets the key identifier whose state was set.
        /// </summary>
        public string SysId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the state now stored for the key.
        /// </summary>
        public bool Enabled { get; set; }
    }
}
