using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the enable / disable API key operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class SetApiKeyEnabledRequest : ApiRequest, ISetApiKeyEnabledRequest
    {
        /// <summary>
        /// Gets or sets the key identifier being enabled or disabled.
        /// </summary>
        public string SysId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the key is accepted from now on.
        /// </summary>
        public bool Enabled { get; set; }
    }
}
