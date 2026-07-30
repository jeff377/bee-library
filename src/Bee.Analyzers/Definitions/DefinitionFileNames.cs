namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// File name conventions for the framework definition files supplied through <c>AdditionalFiles</c>.
    /// </summary>
    internal static class DefinitionFileNames
    {
        /// <summary>
        /// Suffix of a form schema definition file, for example <c>Product.FormSchema.xml</c>.
        /// </summary>
        public const string FormSchemaSuffix = ".FormSchema.xml";

        /// <summary>
        /// Suffix of a table schema definition file, for example <c>ft_product.TableSchema.xml</c>.
        /// </summary>
        public const string TableSchemaSuffix = ".TableSchema.xml";

        /// <summary>
        /// File name of the database category settings, which registers every table under a scope.
        /// </summary>
        public const string DbCategorySettings = "DbCategorySettings.xml";

        /// <summary>
        /// Determines whether the specified path is a form schema definition file.
        /// </summary>
        /// <param name="path">The file path to test.</param>
        /// <returns><c>true</c> when the path ends with the form schema suffix.</returns>
        public static bool IsFormSchema(string path)
            => path.EndsWith(FormSchemaSuffix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether the specified path is the database category settings file.
        /// </summary>
        /// <param name="path">The file path to test.</param>
        /// <returns><c>true</c> when the file name matches the database category settings.</returns>
        public static bool IsDbCategorySettings(string path)
        {
            var separator = path.LastIndexOfAny(new[] { '/', '\\' });
            var fileName = separator >= 0 ? path.Substring(separator + 1) : path;
            return string.Equals(fileName, DbCategorySettings, StringComparison.OrdinalIgnoreCase);
        }
    }
}
