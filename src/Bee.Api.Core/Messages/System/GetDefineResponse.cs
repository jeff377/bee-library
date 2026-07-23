using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get definition operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetDefineResponse : ApiResponse, IGetDefineResponse
    {
        /// <summary>
        /// Gets or sets the definition XML content.
        /// </summary>
        public string Xml { get; set; } = string.Empty;
    }
}
