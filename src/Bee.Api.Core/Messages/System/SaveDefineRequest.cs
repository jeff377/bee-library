using Bee.Definition;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the save definition operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class SaveDefineRequest : ApiRequest, ISaveDefineRequest
    {
        /// <summary>
        /// Gets or sets the definition type.
        /// </summary>
        public DefineType DefineType { get; set; }

        /// <summary>
        /// Gets or sets the definition XML content.
        /// </summary>
        public string Xml { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional filter keys.
        /// </summary>
        public string[]? Keys { get; set; } = null;
    }
}
