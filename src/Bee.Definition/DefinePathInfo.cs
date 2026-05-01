
namespace Bee.Definition
{
    /// <summary>
    /// Provides file path information for define data files.
    /// </summary>
    public static class DefinePathInfo
    {
        /// <summary>
        /// Gets the define data path combined with an optional sub-path.
        /// </summary>
        /// <param name="subPath">The sub-path.</param>
        private static string GetDefinePath(string subPath)
        {
            return Path.Combine(BackendInfo.DefinePath, subPath);
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
        /// Gets the file path for the database table list settings file.
        /// </summary>
        public static string GetDbTableSettingsFilePath()
        {
            string sFileName;

            sFileName = "DbSchemaSettings.xml";
            return GetDefinePath(sFileName);
        }

        /// <summary>
        /// Gets the file path for the specified table schema.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public static string GetTableSchemaFilePath(string dbName, string tableName)
        {
            return GetDefinePath(Path.Combine("TableSchema", dbName, $"{tableName}.TableSchema.xml"));
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
