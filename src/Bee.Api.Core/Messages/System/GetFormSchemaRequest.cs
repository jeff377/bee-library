using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the get form schema operation.
    /// </summary>
    [MessagePackObject]
    public class GetFormSchemaRequest : ApiRequest, IGetFormSchemaRequest
    {
        /// <summary>
        /// Gets or sets the program identifier of the form schema to retrieve.
        /// </summary>
        [Key(100)]
        public string ProgId { get; set; } = string.Empty;
    }
}
