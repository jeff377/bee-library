using Bee.Api.Contracts.System;
using Bee.Definition.Security;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the list API keys operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class ListApiKeysResponse : ApiResponse, IListApiKeysResponse
    {
        /// <summary>
        /// Gets or sets the issued keys, without any credential material.
        /// </summary>
        public List<ApiKeySummary> ApiKeys { get; set; } = [];

        /// <inheritdoc/>
        [IgnoreMember]
        IReadOnlyList<ApiKeySummary> IListApiKeysResponse.ApiKeys => ApiKeys;
    }
}
