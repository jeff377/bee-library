namespace Bee.Definition
{
    /// <summary>
    /// Path-related configuration. Holds <see cref="DefinePath"/> plus convenience
    /// methods that compute the canonical file paths for each definition artifact.
    /// </summary>
    /// <remarks>
    /// Construct once at host startup and inject into consumers via ctor (or DI).
    /// </remarks>
    public class PathOptions
    {
        /// <summary>
        /// Root directory for definition data files
        /// (SystemSettings.xml, DatabaseSettings.xml, FormSchema/, TableSchema/ etc.).
        /// </summary>
        public string DefinePath { get; init; } = string.Empty;

        /// <summary>
        /// Root directory for tenant customization overrides
        /// (<c>{CustomizePath}/{customizeId}/Language/</c>, <c>FormLayout/</c>, <c>ProgramSettings.xml</c>).
        /// Empty means the standard (non-customized) deployment — the customization layer is
        /// skipped entirely and every consumer behaves bit-for-bit like the base layer.
        /// </summary>
        public string CustomizePath { get; init; } = string.Empty;

        /// <summary>Gets the absolute path of <c>SystemSettings.xml</c>.</summary>
        public string GetSystemSettingsFilePath()
            => System.IO.Path.Combine(DefinePath, "SystemSettings.xml");

        /// <summary>Gets the absolute path of <c>DatabaseSettings.xml</c>.</summary>
        public string GetDatabaseSettingsFilePath()
            => System.IO.Path.Combine(DefinePath, "DatabaseSettings.xml");

        /// <summary>Gets the absolute path of <c>ProgramSettings.xml</c>.</summary>
        public virtual string GetProgramSettingsFilePath()
            => System.IO.Path.Combine(DefinePath, "ProgramSettings.xml");

        /// <summary>Gets the absolute path of <c>DbCategorySettings.xml</c>.</summary>
        public string GetDbCategorySettingsFilePath()
            => System.IO.Path.Combine(DefinePath, "DbCategorySettings.xml");

        /// <summary>Gets the absolute path of <c>CurrencySettings.xml</c>.</summary>
        public string GetCurrencySettingsFilePath()
            => System.IO.Path.Combine(DefinePath, "CurrencySettings.xml");

        /// <summary>Gets the absolute path of <c>UnitSettings.xml</c>.</summary>
        public string GetUnitSettingsFilePath()
            => System.IO.Path.Combine(DefinePath, "UnitSettings.xml");

        /// <summary>Gets the absolute path of <c>PermissionModels.xml</c>.</summary>
        public string GetPermissionModelsFilePath()
            => System.IO.Path.Combine(DefinePath, "PermissionModels.xml");

        /// <summary>Gets the absolute path of the TableSchema XML for the given category + table.</summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public string GetTableSchemaFilePath(string categoryId, string tableName)
            => System.IO.Path.Combine(DefinePath, "TableSchema", categoryId, $"{tableName}.TableSchema.xml");

        /// <summary>Gets the absolute path of the FormSchema XML for the given progId.</summary>
        /// <param name="progId">The program ID.</param>
        public string GetFormSchemaFilePath(string progId)
            => System.IO.Path.Combine(DefinePath, "FormSchema", $"{progId}.FormSchema.xml");

        /// <summary>Gets the absolute path of the FormLayout XML for the given layout id.</summary>
        /// <param name="layoutId">The form layout ID.</param>
        public virtual string GetFormLayoutFilePath(string layoutId)
            => System.IO.Path.Combine(DefinePath, "FormLayout", $"{layoutId}.FormLayout.xml");

        /// <summary>Gets the absolute path of the Language XML for the given language code and namespace.</summary>
        /// <param name="lang">The BCP-47 language code (e.g. <c>"zh-TW"</c>).</param>
        /// <param name="ns">The resource namespace (matches file name stem; e.g. <c>"Common"</c>, <c>"Customer"</c>).</param>
        public virtual string GetLanguageFilePath(string lang, string ns)
            => System.IO.Path.Combine(DefinePath, "Language", lang, $"{ns}.Language.xml");
    }
}
