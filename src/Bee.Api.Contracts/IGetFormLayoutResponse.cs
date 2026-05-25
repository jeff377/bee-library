using Bee.Definition.Layouts;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the get form layout response.
    /// </summary>
    public interface IGetFormLayoutResponse
    {
        /// <summary>
        /// Gets the form layout as a typed object (serialised as a JSON tree on
        /// the Plain wire format).
        /// </summary>
        FormLayout? Layout { get; }
    }
}
