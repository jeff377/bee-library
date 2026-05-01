using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get definition operation.
    /// </summary>
    [MessagePackObject]
    public class GetDefineResponse : ApiResponse, IGetDefineResponse
    {
        /// <summary>
        /// Gets or sets the definition XML content.
        /// </summary>
        [Key(100)]
        public string Xml { get; set; } = string.Empty;
    }
}
