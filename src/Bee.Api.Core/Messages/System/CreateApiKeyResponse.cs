using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the create API key operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class CreateApiKeyResponse : ApiResponse, ICreateApiKeyResponse
    {
        /// <summary>
        /// Gets or sets the key identifier that was issued.
        /// </summary>
        public string SysId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the complete plaintext key, returned exactly once.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: not recoverable afterwards — only the hash is stored.
        /// </remarks>
        public string ApiKey { get; set; } = string.Empty;
    }
}
