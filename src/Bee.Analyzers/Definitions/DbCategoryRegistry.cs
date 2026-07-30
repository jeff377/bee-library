using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// The table-to-scope registrations declared in <c>DbCategorySettings.xml</c>.
    /// </summary>
    /// <remarks>
    /// Table names are compared case-insensitively. The framework's own routing is ordinal, but a
    /// casing mismatch between a schema and the registry is a different defect from the one this
    /// registry supports diagnosing, and treating it as "not registered" would report a misleading
    /// cause. Skipping it here keeps the false positive rate down at the cost of not catching casing
    /// drift, which is the right trade for a warning-severity rule.
    /// </remarks>
    internal sealed class DbCategoryRegistry
    {
        private readonly Dictionary<string, HashSet<string>> _tablesByScope;

        private DbCategoryRegistry(Dictionary<string, HashSet<string>> tablesByScope)
        {
            _tablesByScope = tablesByScope;
        }

        /// <summary>
        /// Attempts to build the registry from the database category settings in the additional files.
        /// </summary>
        /// <param name="additionalFiles">The additional files supplied to the compilation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The registry, or <c>null</c> when no readable <c>DbCategorySettings.xml</c> is present.
        /// </returns>
        /// <remarks>
        /// IMPORTANT: Returning <c>null</c> must silence every rule that depends on the registry rather
        /// than downgrade them. Definitions can be stored in the database instead of the file system
        /// (see <c>DbDefineStorage</c>), in which case no definition file exists at all and every
        /// cross-file rule would otherwise report a false positive for every table.
        /// </remarks>
        public static DbCategoryRegistry? TryCreate(
            IEnumerable<AdditionalText> additionalFiles,
            CancellationToken cancellationToken)
        {
            var settingsFile = additionalFiles.FirstOrDefault(file => DefinitionFileNames.IsDbCategorySettings(file.Path));
            if (settingsFile is null)
                return null;

            var text = settingsFile.GetText(cancellationToken);
            if (text is null)
                return null;

            var root = DefinitionDocumentLoader.TryLoad(text)?.Root;
            if (root is null)
                return null;

            var tablesByScope = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var category in root.Descendants("DbCategory"))
            {
                var scope = category.Attribute("Id")?.Value;
                if (string.IsNullOrEmpty(scope))
                    continue;

                if (!tablesByScope.TryGetValue(scope!, out var tables))
                {
                    tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    tablesByScope[scope!] = tables;
                }

                foreach (var item in category.Descendants("TableItem"))
                {
                    var tableName = item.Attribute("TableName")?.Value;
                    if (!string.IsNullOrEmpty(tableName))
                        tables.Add(tableName!);
                }
            }

            return tablesByScope.Count > 0 ? new DbCategoryRegistry(tablesByScope) : null;
        }

        /// <summary>
        /// Determines whether the specified table is registered under the specified scope.
        /// </summary>
        /// <param name="scope">The database scope to look in.</param>
        /// <param name="tableName">The physical table name.</param>
        /// <returns><c>true</c> when the table is registered under that scope.</returns>
        public bool IsRegistered(string scope, string tableName)
            => _tablesByScope.TryGetValue(scope, out var tables) && tables.Contains(tableName);

        /// <summary>
        /// Finds the scope the specified table is actually registered under.
        /// </summary>
        /// <param name="tableName">The physical table name.</param>
        /// <returns>
        /// The scope containing the table, or <c>null</c> when it is not registered anywhere.
        /// </returns>
        /// <remarks>
        /// Naming the scope a table was actually registered under turns "not registered" into an
        /// actionable message: the usual cause is a schema declaring the wrong CategoryId rather than
        /// a genuinely missing registration.
        /// </remarks>
        public string? FindScopeOf(string tableName)
        {
            return _tablesByScope
                .Where(pair => pair.Value.Contains(tableName))
                .Select(pair => pair.Key)
                .FirstOrDefault();
        }
    }
}
