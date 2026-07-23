using Bee.Api.Contracts.System;
using Bee.Definition.Forms;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for retrieving a form schema as a typed object.
    /// </summary>
    public class GetFormSchemaResult : BusinessResult, IGetFormSchemaResponse
    {
        /// <summary>
        /// Gets or sets the form schema as a typed object (serialised as a JSON
        /// tree on the Plain wire format).
        /// </summary>
        public FormSchema? Schema { get; set; }
    }
}
