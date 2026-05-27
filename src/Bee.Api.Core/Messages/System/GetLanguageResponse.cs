using Bee.Api.Contracts;
using Bee.Definition.Language;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get language resource operation.
    /// </summary>
    [MessagePackObject]
    public class GetLanguageResponse : ApiResponse, IGetLanguageResponse
    {
        /// <summary>
        /// Gets or sets the language resource as a typed object (serialised as a
        /// JSON tree on the Plain wire format).
        /// </summary>
        [Key(100)]
        public LanguageResource? Resource { get; set; }
    }
}
