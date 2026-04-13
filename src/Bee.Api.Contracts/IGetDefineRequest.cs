using Bee.Definition;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for get definition request parameters.
    /// </summary>
    public interface IGetDefineRequest
    {
        /// <summary>
        /// Gets the definition type.
        /// </summary>
        DefineType DefineType { get; }

        /// <summary>
        /// Gets the optional filter keys.
        /// </summary>
        string[] Keys { get; }
    }
}
