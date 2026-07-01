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
        /// Program settings list.
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
        UnitSettings
    }
}
