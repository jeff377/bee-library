using Bee.Business.Form;

namespace Bee.Business
{
    /// <summary>
    /// Resolves the concrete <see cref="FormBusinessObject"/>-derived type for a given progId.
    /// </summary>
    /// <remarks>
    /// ERP applications can install a custom resolver (e.g. one backed by an XML mapping
    /// file) to dispatch progId to a specific BO subclass. The framework default
    /// (<see cref="DefaultFormBoTypeResolver"/>) always returns the base
    /// <see cref="FormBusinessObject"/>.
    /// </remarks>
    public interface IFormBoTypeResolver
    {
        /// <summary>
        /// Returns the concrete BO type for the given progId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        Type Resolve(string progId);
    }

    /// <summary>
    /// Default resolver — always returns <see cref="FormBusinessObject"/>.
    /// Reserved for the framework's out-of-the-box behaviour; ERP applications
    /// typically supply a custom resolver.
    /// </summary>
    public sealed class DefaultFormBoTypeResolver : IFormBoTypeResolver
    {
        /// <inheritdoc/>
        public Type Resolve(string progId) => typeof(FormBusinessObject);
    }
}
