using Bee.Definition;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for save definition request parameters.
    /// </summary>
    public interface ISaveDefineRequest
    {
        /// <summary>
        /// Gets the definition type.
        /// </summary>
        DefineType DefineType { get; }

        /// <summary>
        /// Gets the definition XML content.
        /// </summary>
        string Xml { get; }

        /// <summary>
        /// Gets the optional filter keys.
        /// </summary>
        string[] Keys { get; }
    }
}
