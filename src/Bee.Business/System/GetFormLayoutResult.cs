using Bee.Api.Contracts;
using Bee.Definition.Layouts;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for retrieving a form layout as a typed object.
    /// </summary>
    public class GetFormLayoutResult : BusinessResult, IGetFormLayoutResponse
    {
        /// <summary>
        /// Gets or sets the form layout as a typed object (serialised as a JSON
        /// tree on the Plain wire format).
        /// </summary>
        public FormLayout? Layout { get; set; }
    }
}
