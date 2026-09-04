namespace Bee.Business
{
    /// <summary>
    /// Resolves the concrete <see cref="BusinessObject"/>-derived type for a given progId.
    /// </summary>
    /// <remarks>
    /// The framework registers <see cref="ProgramSettingsBoTypeResolver"/> by default via
    /// <c>AddBeeFramework</c>, which looks up <see cref="Bee.Definition.Settings.ProgramItem.BusinessObject"/> in
    /// <c>ProgramSettings.xml</c>. Every business object is resolved this way — the reserved
    /// progIds are registry entries like any other rather than a separate code path. Hosts that
    /// need to bypass <see cref="Bee.Definition.Settings.ProgramSettings"/> entirely can replace the registration with
    /// <see cref="DefaultBoTypeResolver"/> or a custom implementation.
    /// </remarks>
    public interface IBoTypeResolver
    {
        /// <summary>
        /// Returns the concrete BO type for the given progId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        Type Resolve(string progId);

        /// <summary>
        /// Returns the concrete BO type for the given progId, applying the tenant customization
        /// overlay for the supplied customization code (the ProgramSettings overlay is per-progId:
        /// a customization entry wins over the base entry, otherwise the base resolution applies).
        /// </summary>
        /// <param name="customizeId">The tenant customization code; empty resolves against the base layer only.</param>
        /// <param name="progId">The program identifier.</param>
        /// <remarks>
        /// Default implementation ignores <paramref name="customizeId"/> and delegates to
        /// <see cref="Resolve(string)"/> — resolvers without customization support behave exactly
        /// as before. <see cref="ProgramSettingsBoTypeResolver"/> overrides this to overlay.
        /// </remarks>
        Type Resolve(string customizeId, string progId) => Resolve(progId);
    }
}
