using Bee.Definition.Database;

namespace Bee.Db
{
    /// <summary>
    /// Extension methods for <see cref="DatabaseType"/> covering SQL dialect specifics
    /// such as parameter prefix and identifier quoting.
    /// </summary>
    public static class DatabaseTypeExtensions
    {
        /// <summary>
        /// Dictionary mapping database types to their parameter name prefix characters.
        /// </summary>
        private static readonly Dictionary<DatabaseType, string> DbParameterPrefixes = new Dictionary<DatabaseType, string>
        {
            { DatabaseType.SQLServer, "@" },
            { DatabaseType.PostgreSQL, "@" },
            { DatabaseType.MySQL, "@" },
            { DatabaseType.Oracle, ":" },
            { DatabaseType.SQLite, "@" }
        };

        /// <summary>
        /// Dictionary mapping database types to their identifier quoting functions.
        /// </summary>
        private static readonly Dictionary<DatabaseType, Func<string, string>> QuoteIdentifiers = new Dictionary<DatabaseType, Func<string, string>>
        {
            { DatabaseType.SQLServer, s => $"[{s.Replace("]", "]]")}]" },
            { DatabaseType.PostgreSQL, s => $"\"{s.Replace("\"", "\"\"")}\"" },
            { DatabaseType.MySQL, s => $"`{s.Replace("`", "``")}`" },
            { DatabaseType.Oracle, s => $"\"{s.ToUpperInvariant().Replace("\"", "\"\"")}\"" },
            { DatabaseType.SQLite, s => $"\"{s.Replace("\"", "\"\"")}\"" }
        };

        /// <summary>
        /// Gets the parameter name prefix character for the specified database type.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        /// <returns>The parameter prefix character.</returns>
        /// <exception cref="NotSupportedException">Thrown when the database type is not supported.</exception>
        public static string GetParameterPrefix(this DatabaseType databaseType)
        {
            return DbParameterPrefixes.TryGetValue(databaseType, out var prefix)
                ? prefix
                : throw new NotSupportedException($"Unsupported database type: {databaseType}.");
        }

        /// <summary>
        /// Gets the full parameter name including its prefix character.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        /// <param name="name">The parameter name without the prefix character.</param>
        public static string GetParameterName(this DatabaseType databaseType, string name)
        {
            string parameterPrefix = databaseType.GetParameterPrefix();
            return string.IsNullOrEmpty(parameterPrefix) ? name : parameterPrefix + name;
        }

        /// <summary>
        /// Returns the properly quoted identifier string for the specified database type.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        /// <param name="identifier">The identifier name to quote.</param>
        /// <returns>The quoted identifier.</returns>
        /// <exception cref="NotSupportedException">Thrown when the database type is not supported.</exception>
        public static string QuoteIdentifier(this DatabaseType databaseType, string identifier)
        {
            if (QuoteIdentifiers.TryGetValue(databaseType, out var func))
                return func(identifier);
            throw new NotSupportedException($"Unsupported database type: {databaseType}.");
        }
    }
}
