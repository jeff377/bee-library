using Bee.Api.Contracts.System;
using Bee.Definition.Security;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for issuing an API key.
    /// </summary>
    public class CreateApiKeyArgs : BusinessArgs, ICreateApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the key identifier to issue, which becomes the leading segment of the
        /// plaintext key.
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
