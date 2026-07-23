using Bee.Api.Contracts.System;
using Bee.Definition.Language;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get language resource operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetLanguageResponse : ApiResponse, IGetLanguageResponse
    {
        /// <summary>
        /// Gets or sets the language resource as a typed object (serialised as a
        /// JSON tree on the Plain wire format).
        /// </summary>
        public LanguageResource? Resource { get; set; }
    }
}
