using Bee.Business.Form;

namespace Bee.Business
{
    /// <summary>
    /// Resolves the concrete <see cref="FormBusinessObject"/>-derived type for a given progId.
    /// </summary>
    /// <remarks>
    /// The framework registers <see cref="ProgramSettingsFormBoTypeResolver"/> by default
    /// via <c>AddBeeFramework</c>, which looks up <c>ProgramItem.BusinessObject</c> from
    /// <c>ProgramSettings.xml</c>. Hosts that need to bypass <c>ProgramSettings</c>
    /// entirely can replace the registration with <see cref="DefaultFormBoTypeResolver"/>
    /// or a custom implementation.
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
    /// Minimal resolver — always returns <see cref="FormBusinessObject"/>.
    /// Reserved for tests and hosts that intentionally bypass
    /// <see cref="ProgramSettingsFormBoTypeResolver"/>; the framework default is
    /// <see cref="ProgramSettingsFormBoTypeResolver"/>, wired up by <c>AddBeeFramework</c>.
    /// </summary>
    public sealed class DefaultFormBoTypeResolver : IFormBoTypeResolver
    {
        /// <inheritdoc/>
        public Type Resolve(string progId) => typeof(FormBusinessObject);
    }
}
