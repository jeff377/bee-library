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

        /// <summary>
        /// Returns the concrete BO type for the given progId, applying the tenant customization
        /// overlay for the supplied customization code (ProgramSettings overlay is per-progId:
        /// a customization entry wins over the base entry, otherwise the base resolution applies).
        /// </summary>
        /// <param name="customizeId">The tenant customization code; empty resolves against the base layer only.</param>
        /// <param name="progId">The program identifier.</param>
        /// <remarks>
        /// Default implementation ignores <paramref name="customizeId"/> and delegates to
        /// <see cref="Resolve(string)"/> — resolvers without customization support behave exactly
        /// as before. <see cref="ProgramSettingsFormBoTypeResolver"/> overrides this to overlay.
        /// </remarks>
        Type Resolve(string customizeId, string progId) => Resolve(progId);
    }
}
