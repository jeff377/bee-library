namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// File name and folder conventions for the framework definition files supplied through
    /// <c>AdditionalFiles</c>.
    /// </summary>
    internal static class DefinitionFileNames
    {
        private static readonly char[] s_separators = { '/', '\\' };

        /// <summary>
        /// Suffix of a form schema definition file, for example <c>Product.FormSchema.xml</c>.
        /// </summary>
        public const string FormSchemaSuffix = ".FormSchema.xml";

        /// <summary>
        /// Suffix of a table schema definition file, for example <c>ft_product.TableSchema.xml</c>.
        /// </summary>
        public const string TableSchemaSuffix = ".TableSchema.xml";

        /// <summary>
        /// Suffix of a form layout definition file, for example <c>Product.FormLayout.xml</c>.
        /// </summary>
        public const string FormLayoutSuffix = ".FormLayout.xml";

        /// <summary>
        /// Suffix of a language resource file, for example <c>Product.Language.xml</c>.
        /// </summary>
        public const string LanguageSuffix = ".Language.xml";

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
        /// Determines whether the specified path is a table schema definition file.
        /// </summary>
        /// <param name="path">The file path to test.</param>
        /// <returns><c>true</c> when the path ends with the table schema suffix.</returns>
        public static bool IsTableSchema(string path)
            => path.EndsWith(TableSchemaSuffix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether the specified path is a form layout definition file.
        /// </summary>
        /// <param name="path">The file path to test.</param>
        /// <returns><c>true</c> when the path ends with the form layout suffix.</returns>
        public static bool IsFormLayout(string path)
            => path.EndsWith(FormLayoutSuffix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether the specified path is a language resource file.
        /// </summary>
        /// <param name="path">The file path to test.</param>
        /// <returns><c>true</c> when the path ends with the language suffix.</returns>
        public static bool IsLanguage(string path)
            => path.EndsWith(LanguageSuffix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether the specified path is the database category settings file.
        /// </summary>
        /// <param name="path">The file path to test.</param>
        /// <returns><c>true</c> when the file name matches the database category settings.</returns>
        public static bool IsDbCategorySettings(string path)
            => string.Equals(GetFileName(path), DbCategorySettings, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the identifier a sidecar definition file is named after.
        /// </summary>
        /// <param name="path">The path of the definition file.</param>
        /// <param name="suffix">The suffix to strip, for example <see cref="FormSchemaSuffix"/>.</param>
        /// <returns>
        /// The file name with its directory and the suffix removed, for example <c>Product</c> for
        /// <c>Define/FormSchema/Product.FormSchema.xml</c>.
        /// </returns>
        public static string GetProgIdFromSidecar(string path, string suffix)
        {
            var fileName = GetFileName(path);
            var index = fileName.LastIndexOf(suffix, StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? fileName.Substring(0, index) : fileName;
        }

        /// <summary>
        /// Gets the lookup key identifying a table schema by the scope folder it lives in.
        /// </summary>
        /// <param name="path">The path of a table schema definition file.</param>
        /// <returns>
        /// The composed key, or <c>null</c> when the file is not inside a scope folder.
        /// </returns>
        /// <remarks>
        /// The containing folder is significant, not cosmetic: table schemas are resolved as
        /// <c>TableSchema/&lt;categoryId&gt;/&lt;tableName&gt;.TableSchema.xml</c>, so a schema filed under
        /// the wrong scope folder is not found at run time even though the file exists.
        /// </remarks>
        public static string? GetTableSchemaKey(string path)
        {
            var scope = GetParentFolderName(path);
            return scope is null
                ? null
                : ComposeTableSchemaKey(scope, GetProgIdFromSidecar(path, TableSchemaSuffix));
        }

        /// <summary>
        /// Composes the lookup key for a table schema.
        /// </summary>
        /// <param name="categoryId">The database scope, which is also the folder name.</param>
        /// <param name="tableName">The physical table name.</param>
        /// <returns>The composed key.</returns>
        public static string ComposeTableSchemaKey(string categoryId, string tableName)
            => categoryId + "|" + tableName;

        /// <summary>
        /// Gets the name of the folder immediately containing the specified file.
        /// </summary>
        /// <param name="path">The file path to inspect.</param>
        /// <returns>
        /// The folder name, or <c>null</c> when the path has no containing folder.
        /// </returns>
        /// <remarks>
        /// The containing folder carries meaning for two definition kinds: it is the database scope for
        /// a table schema (<c>TableSchema/company/...</c>) and the culture for a language resource
        /// (<c>Language/zh-TW/...</c>). Both resolve to this one lookup.
        /// </remarks>
        public static string? GetParentFolderName(string path)
        {
            var separator = path.LastIndexOfAny(s_separators);
            if (separator <= 0)
                return null;

            var directory = path.Substring(0, separator);
            var parentSeparator = directory.LastIndexOfAny(s_separators);
            var name = parentSeparator >= 0 ? directory.Substring(parentSeparator + 1) : directory;
            return name.Length > 0 ? name : null;
        }

        private static string GetFileName(string path)
        {
            var separator = path.LastIndexOfAny(s_separators);
            return separator >= 0 ? path.Substring(separator + 1) : path;
        }
    }
}
