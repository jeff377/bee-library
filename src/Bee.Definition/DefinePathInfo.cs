
namespace Bee.Definition
{
    /// <summary>
    /// Provides file path information for define data files.
    /// </summary>
    /// <remarks>
    /// Internally holds a <see cref="PathOptions"/> instance installed via
    /// <see cref="Initialize"/> (typically called at host startup, or by test
    /// helpers such as <c>TempDefinePath</c>). The static API surface is
    /// preserved; callers do not change.
    /// </remarks>
    public static class DefinePathInfo
    {
        private static PathOptions _options = new();

        /// <summary>
        /// The current path options snapshot. Exposed primarily for test helpers
        /// (e.g. <c>TempDefinePath</c>) that need to save and restore state.
        /// </summary>
        public static PathOptions CurrentOptions => _options;

        /// <summary>
        /// Installs the path options. Typically called once at host startup;
        /// test helpers may call this transiently to swap and restore paths.
        /// </summary>
        /// <param name="options">The path options.</param>
        public static void Initialize(PathOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets the define data path combined with an optional sub-path.
        /// </summary>
        /// <param name="subPath">The sub-path.</param>
        private static string GetDefinePath(string subPath)
        {
            return Path.Combine(_options.DefinePath, subPath);
        }

        /// <summary>
        /// Gets the file path for the system settings file.
        /// </summary>
        public static string GetSystemSettingsFilePath()
        {
            string sFileName;

            sFileName = "SystemSettings.xml";
            return GetDefinePath(sFileName);
        }

        /// <summary>
        /// Gets the file path for the database settings file.
        /// </summary>
        public static string GetDatabaseSettingsFilePath()
        {
            string sFileName;

            sFileName = "DatabaseSettings.xml";
            return GetDefinePath(sFileName);
        }

        /// <summary>
        /// Gets the file path for the program settings file.
        /// </summary>
        public static string GetProgramSettingsFilePath()
        {
            string sFileName;

            sFileName = "ProgramSettings.xml";
            return GetDefinePath(sFileName);
        }

        /// <summary>
        /// Gets the file path for the database category settings file.
        /// </summary>
        public static string GetDbCategorySettingsFilePath()
        {
            string sFileName;

            sFileName = "DbCategorySettings.xml";
            return GetDefinePath(sFileName);
        }

        /// <summary>
        /// Gets the file path for the specified table schema.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public static string GetTableSchemaFilePath(string categoryId, string tableName)
        {
            return GetDefinePath(Path.Combine("TableSchema", categoryId, $"{tableName}.TableSchema.xml"));
        }

        /// <summary>
        /// Gets the file path for the specified form schema.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        public static string GetFormSchemaFilePath(string progId)
        {
            return GetDefinePath(Path.Combine("FormSchema", $"{progId}.FormSchema.xml"));
        }

        /// <summary>
        /// Gets the file path for the specified form layout.
        /// </summary>
        /// <param name="layoutId">The form layout ID.</param>
        public static string GetFormLayoutFilePath(string layoutId)
        {
            return GetDefinePath(Path.Combine("FormLayout", $"{layoutId}.FormLayout.xml"));
        }
    }
}
