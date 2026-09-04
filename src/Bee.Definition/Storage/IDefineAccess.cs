using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
namespace Bee.Definition.Storage
{
    /// <summary>
    /// Interface for accessing define data.
    /// </summary>
    public interface IDefineAccess
    {
        /// <summary>
        /// Gets define data.
        /// </summary>
        /// <param name="defineType">The define data type.</param>
        /// <param name="keys">The keys used to retrieve the define data.</param>
        object GetDefine(DefineType defineType, string[]? keys = null);

        /// <summary>
        /// Saves define data.
        /// </summary>
        /// <param name="defineType">The define data type.</param>
        /// <param name="defineObject">The define data object.</param>
        /// <param name="keys">The keys used to save the define data.</param>
        void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null);

        /// <summary>
        /// Gets the system settings.
        /// </summary>
        SystemSettings GetSystemSettings();

        /// <summary>
        /// Saves the system settings.
        /// </summary>
        /// <param name="settings">The system settings.</param>
        void SaveSystemSettings(SystemSettings settings);

        /// <summary>
        /// Gets the database settings.
        /// </summary>
        DatabaseSettings GetDatabaseSettings();

        /// <summary>
        /// Saves the database settings.
        /// </summary>
        /// <param name="settings">The database settings.</param>
        void SaveDatabaseSettings(DatabaseSettings settings);

        /// <summary>
        /// Gets the program settings.
        /// </summary>
        ProgramSettings GetProgramSettings();

        /// <summary>
        /// Saves the program settings.
        /// </summary>
        /// <param name="settings">The program settings.</param>
        void SaveProgramSettings(ProgramSettings settings);

        /// <summary>
        /// Gets the base-layer menu definition.
        /// </summary>
        /// <remarks>
        /// Default implementation delegates to <c>GetDefine</c>; <c>CacheDefineAccess</c>
        /// overrides it with its cache path.
        /// </remarks>
        MenuSettings GetMenuSettings() => (MenuSettings)GetDefine(DefineType.MenuSettings);

        /// <summary>
        /// Gets the menu definition for the supplied customization code. The customization menu
        /// replaces the base menu outright — a menu is one arrangement, so per-node merging would
        /// produce combinations no author intended.
        /// </summary>
        /// <param name="customizeId">The tenant customization code; empty resolves against the base layer only.</param>
        /// <remarks>
        /// Default implementation ignores <paramref name="customizeId"/> and delegates to
        /// <see cref="GetMenuSettings()"/> — an access layer without customization support behaves
        /// as the base layer. <c>CacheDefineAccess</c> overrides this to overlay.
        /// </remarks>
        MenuSettings GetMenuSettings(string customizeId) => GetMenuSettings();

        /// <summary>
        /// Saves the menu definition.
        /// </summary>
        /// <param name="settings">The menu definition.</param>
        void SaveMenuSettings(MenuSettings settings) => SaveDefine(DefineType.MenuSettings, settings);

        /// <summary>
        /// Gets the base-layer business plugin bindings.
        /// </summary>
        /// <remarks>
        /// Default implementation delegates to <c>GetDefine</c>; <c>CacheDefineAccess</c>
        /// overrides it with its cache path.
        /// </remarks>
        /// <remarks>
        /// No customization-aware overload, matching <see cref="GetProgramSettings"/>: the two
        /// layers add up per progId, and a consumer needs the chain of the one program it is
        /// running, not a merge of every program. It reads this base copy, asks
        /// <see cref="ICustomizeDefineReader"/> for the tenant copy, and combines them for its progId with
        /// <see cref="Bee.Definition.Customization.CustomizeOverlay.GetPluginTypes"/> — the same routine a client would run, so both
        /// ends agree.
        /// </remarks>
        PluginSettings GetPluginSettings() => (PluginSettings)GetDefine(DefineType.PluginSettings);

        /// <summary>
        /// Saves the business plugin bindings.
        /// </summary>
        /// <param name="settings">The plugin bindings.</param>
        void SavePluginSettings(PluginSettings settings) => SaveDefine(DefineType.PluginSettings, settings);

        /// <summary>
        /// Gets the permission model registry.
        /// </summary>
        /// <remarks>
        /// Default implementation delegates to <c>GetDefine</c>; <c>CacheDefineAccess</c>
        /// overrides it with its cache path.
        /// </remarks>
        PermissionModels GetPermissionModels() => (PermissionModels)GetDefine(DefineType.PermissionModels);

        /// <summary>
        /// Saves the permission model registry.
        /// </summary>
        /// <param name="models">The permission model registry.</param>
        void SavePermissionModels(PermissionModels models) => SaveDefine(DefineType.PermissionModels, models);

        /// <summary>
        /// Gets the system-level currency master.
        /// </summary>
        /// <remarks>
        /// Default implementation delegates to <c>GetDefine</c>; <c>CacheDefineAccess</c>
        /// overrides it with its cache path.
        /// </remarks>
        CurrencySettings GetCurrencySettings() => (CurrencySettings)GetDefine(DefineType.CurrencySettings);

        /// <summary>
        /// Saves the system-level currency master.
        /// </summary>
        /// <param name="settings">The currency master.</param>
        void SaveCurrencySettings(CurrencySettings settings) => SaveDefine(DefineType.CurrencySettings, settings);

        /// <summary>
        /// Gets the system-level unit-of-measure master.
        /// </summary>
        /// <remarks>
        /// Default implementation delegates to <c>GetDefine</c>; <c>CacheDefineAccess</c>
        /// overrides it with its cache path.
        /// </remarks>
        UnitSettings GetUnitSettings() => (UnitSettings)GetDefine(DefineType.UnitSettings);

        /// <summary>
        /// Saves the system-level unit-of-measure master.
        /// </summary>
        /// <param name="settings">The unit master.</param>
        void SaveUnitSettings(UnitSettings settings) => SaveDefine(DefineType.UnitSettings, settings);

        /// <summary>
        /// Gets the database category settings.
        /// </summary>
        DbCategorySettings GetDbCategorySettings();

        /// <summary>
        /// Saves the database category settings.
        /// </summary>
        /// <param name="settings">The database category settings.</param>
        void SaveDbCategorySettings(DbCategorySettings settings);

        /// <summary>
        /// Gets the table schema for the specified category and table.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        TableSchema GetTableSchema(string categoryId, string tableName);

        /// <summary>
        /// Saves the table schema for the specified category.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableSchema">The table schema.</param>
        void SaveTableSchema(string categoryId, TableSchema tableSchema);

        /// <summary>
        /// Gets the form schema for the specified program.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        FormSchema GetFormSchema(string progId);

        /// <summary>
        /// Saves the form schema.
        /// </summary>
        /// <param name="formSchema">The form schema.</param>
        void SaveFormSchema(FormSchema formSchema);

        /// <summary>
        /// Gets the form layout for the specified layout ID.
        /// </summary>
        /// <param name="layoutId">The form layout ID.</param>
        FormLayout GetFormLayout(string layoutId);

        /// <summary>
        /// Gets the form layout for the specified layout ID, applying the tenant customization
        /// overlay for the supplied customization code (whole-file selection: a customization
        /// layout file wins outright, otherwise the base layout is returned).
        /// </summary>
        /// <param name="customizeId">The tenant customization code; empty resolves against the base layer only.</param>
        /// <param name="layoutId">The form layout ID.</param>
        /// <remarks>
        /// Default implementation ignores <paramref name="customizeId"/> and delegates to
        /// <see cref="GetFormLayout(string)"/>. <c>CacheDefineAccess</c> overrides this to overlay;
        /// remote access already receives the server-side-overlaid result.
        /// </remarks>
        FormLayout GetFormLayout(string customizeId, string layoutId) => GetFormLayout(layoutId);

        /// <summary>
        /// Looks up the form layout <b>definition</b> for the supplied customization code and
        /// layout ID, returning <c>null</c> when neither layer stores one.
        /// </summary>
        /// <param name="customizeId">The tenant customization code; empty resolves against the base layer only.</param>
        /// <param name="layoutId">The form layout ID.</param>
        /// <returns>The customization layout, else the base layout, else <c>null</c>.</returns>
        /// <remarks>
        /// The optional counterpart of <see cref="GetFormLayout(string, string)"/>, for callers that
        /// need to distinguish "neither layer stores one" from a fault. The runtime layout path
        /// calls this and reports a <c>null</c> result as a configuration error: a form renders its
        /// stored layout definition, and one is never generated on the fly.
        /// <para>
        /// Default implementation returns <c>null</c> — an access layer that offers no optional
        /// lookup reports "no definition", which the runtime path then surfaces as the same
        /// configuration error.
        /// </para>
        /// </remarks>
        FormLayout? FindFormLayout(string customizeId, string layoutId) => null;

        /// <summary>
        /// Saves the form layout.
        /// </summary>
        /// <param name="formLayout">The form layout.</param>
        void SaveFormLayout(FormLayout formLayout);

        /// <summary>
        /// Gets the language resource for the specified language and namespace.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="ns">The resource namespace (matches the file name stem).</param>
        LanguageResource GetLanguage(string lang, string ns);

        /// <summary>
        /// Saves the language resource.
        /// </summary>
        /// <param name="resource">The language resource.</param>
        void SaveLanguage(LanguageResource resource);
    }
}
