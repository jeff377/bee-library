namespace Bee.Definition
{
    /// <summary>
    /// Definition data type.
    /// </summary>
    public enum DefineType
    {
        /// <summary>
        /// System settings.
        /// </summary>
        SystemSettings,
        /// <summary>
        /// Database settings.
        /// </summary>
        DatabaseSettings,
        /// <summary>
        /// Database category settings.
        /// </summary>
        DbCategorySettings,
        /// <summary>
        /// Program type registry (progId to business object / repository bindings).
        /// </summary>
        ProgramSettings,
        /// <summary>
        /// Table schema.
        /// </summary>
        TableSchema,
        /// <summary>
        /// Form schema definition.
        /// </summary>
        FormSchema,
        /// <summary>
        /// Form layout configuration.
        /// </summary>
        FormLayout,
        /// <summary>
        /// Language resource (localized text and enum entries for one namespace × one language).
        /// </summary>
        Language,
        /// <summary>
        /// Permission model registry (model + actions + record-scope strategies).
        /// </summary>
        PermissionModels,
        /// <summary>
        /// System-level currency master (currency decimals and natural minor units).
        /// </summary>
        CurrencySettings,
        /// <summary>
        /// System-level unit-of-measure master (unit display decimals).
        /// </summary>
        UnitSettings,
        /// <summary>
        /// Menu definition (the presentation tree referencing registered programs).
        /// </summary>
        /// <remarks>
        /// Appended rather than placed next to <see cref="ProgramSettings"/>, which reads better:
        /// the values travel on the wire, so renumbering the existing members would be a change
        /// with no upside.
        /// </remarks>
        MenuSettings
    }
}
