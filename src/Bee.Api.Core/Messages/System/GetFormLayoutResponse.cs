using Bee.Api.Contracts;
using Bee.Definition.Layouts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get form layout operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetFormLayoutResponse : ApiResponse, IGetFormLayoutResponse
    {
        /// <summary>
        /// Gets or sets the form layout as a typed object (serialised as a JSON
        /// tree on the Plain wire format).
        /// </summary>
        public FormLayout? Layout { get; set; }
    }
}
