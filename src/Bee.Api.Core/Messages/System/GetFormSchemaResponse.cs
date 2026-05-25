using Bee.Api.Contracts;
using Bee.Definition.Forms;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get form schema operation.
    /// </summary>
    [MessagePackObject]
    public class GetFormSchemaResponse : ApiResponse, IGetFormSchemaResponse
    {
        /// <summary>
        /// Gets or sets the form schema as a typed object (serialised as a JSON
        /// tree on the Plain wire format).
        /// </summary>
        [Key(100)]
        public FormSchema? Schema { get; set; }
    }
}
