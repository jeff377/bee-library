namespace Bee.Definition
{
    /// <summary>
    /// Transitional static facade over <see cref="PathOptions"/> file-path methods.
    /// Phase 5 PR 5.2 moved the canonical path computations onto <see cref="PathOptions"/>;
    /// this class delegates to the currently installed <see cref="PathOptions"/> instance
    /// so existing static callers (cache layer + test helpers) keep working.
    /// Removed in PR 5.4 along with <c>TempDefinePath</c> when the test fixture rewrite
    /// switches everything to ctor-injected <see cref="PathOptions"/>.
    /// </summary>
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

        /// <summary>Gets the absolute path of <c>SystemSettings.xml</c>.</summary>
        public static string GetSystemSettingsFilePath() => _options.GetSystemSettingsFilePath();

        /// <summary>Gets the absolute path of <c>DatabaseSettings.xml</c>.</summary>
        public static string GetDatabaseSettingsFilePath() => _options.GetDatabaseSettingsFilePath();

        /// <summary>Gets the absolute path of <c>ProgramSettings.xml</c>.</summary>
        public static string GetProgramSettingsFilePath() => _options.GetProgramSettingsFilePath();

        /// <summary>Gets the absolute path of <c>DbCategorySettings.xml</c>.</summary>
        public static string GetDbCategorySettingsFilePath() => _options.GetDbCategorySettingsFilePath();

        /// <summary>Gets the absolute path of the TableSchema XML for the given category + table.</summary>
        public static string GetTableSchemaFilePath(string categoryId, string tableName)
            => _options.GetTableSchemaFilePath(categoryId, tableName);

        /// <summary>Gets the absolute path of the FormSchema XML for the given progId.</summary>
        public static string GetFormSchemaFilePath(string progId) => _options.GetFormSchemaFilePath(progId);

        /// <summary>Gets the absolute path of the FormLayout XML for the given layout id.</summary>
        public static string GetFormLayoutFilePath(string layoutId) => _options.GetFormLayoutFilePath(layoutId);
    }
}
