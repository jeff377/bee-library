using Bee.Api.Contracts.System;
using Bee.Definition.Security;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the create API key operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class CreateApiKeyRequest : ApiRequest, ICreateApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the key identifier to issue.
        /// </summary>
        public string SysId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the application this key is for.
        /// </summary>
        public string SysName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key classification.
        /// </summary>
        public ApiKeyType KeyType { get; set; } = ApiKeyType.Internal;

        /// <summary>
        /// Gets or sets the contact for the third party holding this key.
        /// </summary>
        public string? Contact { get; set; }

        /// <summary>
        /// Gets or sets the UTC expiry, or <c>null</c> for a key that does not expire.
        /// </summary>
        public DateTime? ExpiredAt { get; set; }
    }
}
