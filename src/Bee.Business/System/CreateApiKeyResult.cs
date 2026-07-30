using Bee.Api.Contracts.System;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for issuing an API key.
    /// </summary>
    public class CreateApiKeyResult : BusinessResult, ICreateApiKeyResponse
    {
        /// <summary>
        /// Gets or sets the key identifier that was issued.
        /// </summary>
        public string SysId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the complete plaintext key, returned exactly once.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: not recoverable afterwards — only the hash is stored. Callers must persist it
        /// at this point or issue a replacement.
        /// </remarks>
        public string ApiKey { get; set; } = string.Empty;
    }
}
