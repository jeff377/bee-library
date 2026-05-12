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

        /// <summary>Gets the absolute path of <c>SystemSettings.xml</c>.</summary>
        public string GetSystemSettingsFilePath()
            => System.IO.Path.Combine(DefinePath, "SystemSettings.xml");

        /// <summary>Gets the absolute path of <c>DatabaseSettings.xml</c>.</summary>
        public string GetDatabaseSettingsFilePath()
            => System.IO.Path.Combine(DefinePath, "DatabaseSettings.xml");

        /// <summary>Gets the absolute path of <c>ProgramSettings.xml</c>.</summary>
        public string GetProgramSettingsFilePath()
            => System.IO.Path.Combine(DefinePath, "ProgramSettings.xml");

        /// <summary>Gets the absolute path of <c>DbCategorySettings.xml</c>.</summary>
        public string GetDbCategorySettingsFilePath()
            => System.IO.Path.Combine(DefinePath, "DbCategorySettings.xml");

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
        public string GetFormLayoutFilePath(string layoutId)
            => System.IO.Path.Combine(DefinePath, "FormLayout", $"{layoutId}.FormLayout.xml");
    }
}
