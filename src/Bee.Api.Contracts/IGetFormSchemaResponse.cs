using Bee.Definition.Forms;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the get form schema response.
    /// </summary>
    public interface IGetFormSchemaResponse
    {
        /// <summary>
        /// Gets the form schema as a typed object (serialised as a JSON tree on
        /// the Plain wire format).
        /// </summary>
        FormSchema? Schema { get; }
    }
}
