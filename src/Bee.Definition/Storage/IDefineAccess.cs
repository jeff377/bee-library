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
        /// Gets the permission model registry.
        /// </summary>
        /// <remarks>
        /// Default implementation delegates to <see cref="GetDefine"/>; <c>CacheDefineAccess</c>
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
        /// Default implementation delegates to <see cref="GetDefine"/>; <c>CacheDefineAccess</c>
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
        /// Default implementation delegates to <see cref="GetDefine"/>; <c>CacheDefineAccess</c>
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
