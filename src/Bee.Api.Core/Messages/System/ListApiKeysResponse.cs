using Bee.Api.Contracts.System;
using Bee.Definition.Security;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the list API keys operation.
    /// </summary>
    public class ListApiKeysResponse : ApiResponse, IListApiKeysResponse
    {
        /// <summary>
        /// Gets or sets the issued keys, without any credential material.
        /// </summary>
        public List<ApiKeySummary> ApiKeys { get; set; } = [];

        /// <inheritdoc/>
        IReadOnlyList<ApiKeySummary> IListApiKeysResponse.ApiKeys => ApiKeys;
    }
}
