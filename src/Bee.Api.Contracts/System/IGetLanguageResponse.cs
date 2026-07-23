using Bee.Definition.Language;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the get language resource response.
    /// </summary>
    public interface IGetLanguageResponse
    {
        /// <summary>
        /// Gets the language resource as a typed object (serialised as a JSON
        /// tree on the Plain wire format). <c>null</c> when no resource exists
        /// for the requested <c>(Lang, Namespace)</c> pair.
        /// </summary>
        LanguageResource? Resource { get; }
    }
}
