using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the get language resource operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetLanguageRequest : ApiRequest, IGetLanguageRequest
    {
        /// <summary>
        /// Gets or sets the BCP-47 language code (e.g. <c>"zh-TW"</c>, <c>"en-US"</c>).
        /// </summary>
        public string Lang { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource namespace.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;
    }
}
