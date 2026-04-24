using System.Data;
using System.Data.Common;
using Bee.Base;
using Bee.Db.Manager;
using Bee.Definition;

namespace Bee.Db
{
    /// <summary>
    /// Utility library for database access operations.
    /// </summary>
    public static class DbFunc
    {
        /// <summary>
        /// Dictionary mapping database types to their parameter name prefix characters.
        /// </summary>
        private static readonly Dictionary<DatabaseType, string> DbParameterPrefixes = new Dictionary<DatabaseType, string>
        {
            { DatabaseType.SQLServer, "@" },
            { DatabaseType.MySQL, "@" },
            { DatabaseType.SQLite, "@" },
            { DatabaseType.Oracle, ":" }
        };

        /// <summary>
        /// Gets the parameter name prefix character for the specified database type.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        /// <returns>The parameter prefix character.</returns>
        public static string GetParameterPrefix(DatabaseType databaseType)
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
        public static string GetParameterName(DatabaseType databaseType, string name)
        {
            string parameterPrefix = GetParameterPrefix(databaseType);
            return string.IsNullOrEmpty(parameterPrefix) ? name : parameterPrefix + name;
        }

        /// <summary>
        /// Dictionary mapping database types to their identifier quoting functions.
        /// </summary>
        private static readonly Dictionary<DatabaseType, Func<string, string>> QuoteIdentifiers = new Dictionary<DatabaseType, Func<string, string>>
        {
            { DatabaseType.SQLServer, s => $"[{s.Replace("]", "]]")}]" },
            { DatabaseType.MySQL, s => $"`{s.Replace("`", "``")}`" },
            { DatabaseType.SQLite, s => $"\"{s.Replace("\"", "\"\"")}\"" },
            { DatabaseType.Oracle, s => $"\"{s.Replace("\"", "\"\"")}\"" }
        };

        /// <summary>
        /// Returns the properly quoted identifier string for the specified database type.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        /// <param name="identifier">The identifier name to quote.</param>
        /// <returns>The quoted identifier.</returns>
        public static string QuoteIdentifier(DatabaseType databaseType, string identifier)
        {
            if (QuoteIdentifiers.TryGetValue(databaseType, out var func))
                return func(identifier);
            throw new NotSupportedException($"Unsupported database type: {databaseType}.");
        }

        /// <summary>
        /// Infers the <see cref="DbType"/> from the given value.
        /// </summary>
        /// <param name="value">The value to infer the type from.</param>
        public static DbType? InferDbType(object value)
        {
            if (value == null || value is DBNull) return null;

            var type = value.GetType();

            if (type == typeof(string)) return DbType.String;
            if (type == typeof(int)) return DbType.Int32;
            if (type == typeof(long)) return DbType.Int64;
            if (type == typeof(short)) return DbType.Int16;
            if (type == typeof(byte)) return DbType.Byte;
            if (type == typeof(bool)) return DbType.Boolean;
            if (type == typeof(DateTime)) return DbType.DateTime;
            if (type == typeof(decimal)) return DbType.Decimal;
            if (type == typeof(double)) return DbType.Double;
            if (type == typeof(float)) return DbType.Single;
            if (type == typeof(Guid)) return DbType.Guid;
            if (type == typeof(byte[])) return DbType.Binary;
            if (type == typeof(TimeSpan)) return DbType.Time;

            // Fallback: let the provider infer the type automatically
            return null;
        }

        /// <summary>
        /// Creates a database connection for the specified database identifier.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public static DbConnection CreateConnection(string databaseId)
        {
            var connInfo = DbConnectionManager.GetConnectionInfo(databaseId);

            var provider = DbProviderManager.GetFactory(connInfo.DatabaseType)
                    ?? throw new InvalidOperationException($"Unknown database type: {connInfo.DatabaseType}.");
            var connection = provider.CreateConnection()
                    ?? throw new InvalidOperationException("Failed to create a database connection: DbProviderFactory.CreateConnection() returned null.");
            connection.ConnectionString = connInfo.ConnectionString;
            return connection;
        }

        /// <summary>
        /// Formats a SQL command text by substituting parameter names.
        /// </summary>
        /// <param name="s">The SQL command text.</param>
        /// <param name="parameters">The command parameter collection.</param>
        public static string SqlFormat(string s, DbParameterCollection parameters)
        {
            string[] oArgs;

            oArgs = new string[parameters.Count];
            for (int N1 = 0; N1 < parameters.Count; N1++)
                oArgs[N1] = parameters[N1].ParameterName;
            return StrFunc.Format(s, oArgs);
        }

    }
}
