using Bee.Base;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// Shared SQLite identifier, literal, and column-definition primitives used by the
    /// CREATE and ALTER schema builders. Counterpart to <see cref="PostgreSql.PgSchemaSyntax"/>.
    /// </summary>
    internal static class SqliteSchemaSyntax
    {
        /// <summary>
        /// Quotes a SQLite identifier by escaping <c>"</c> as <c>""</c> and wrapping in double quotes.
        /// SQLite accepts the same double-quoted form as PostgreSQL.
        /// </summary>
        /// <param name="identifier">The identifier to quote.</param>
        public static string QuoteName(string identifier)
        {
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// Escapes a string value for use inside a '...' literal by doubling single quotes.
        /// </summary>
        /// <param name="value">The string value to escape.</param>
        public static string EscapeSqlString(string value)
        {
            return value.Replace("'", "''");
        }

        /// <summary>
        /// Gets the SQLite built-in default value expression for the specified field type.
        /// SQLite has no native UUID generator; a hex-of-randomblob expression is used as a
        /// unique-but-not-strictly-v4 surrogate (sufficient for framework-managed defaults).
        /// </summary>
        /// <param name="dbType">The field data type.</param>
        public static string GetDefaultValueExpression(FieldDbType dbType)
        {
            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return string.Empty;
                case FieldDbType.Boolean:
                case FieldDbType.Short:
                case FieldDbType.Integer:
                case FieldDbType.Long:
                case FieldDbType.Decimal:
                case FieldDbType.Currency:
                    return "0";
                case FieldDbType.Date:
                case FieldDbType.DateTime:
                    return "CURRENT_TIMESTAMP";
                case FieldDbType.Guid:
                    return "(hex(randomblob(16)))";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the default value expression for a field, honoring <see cref="DbField.AllowNull"/>
        /// (nullable fields have no default). Returns an empty string when no default should be applied.
        /// </summary>
        /// <param name="field">The field definition.</param>
        public static string GetDefaultExpression(DbField field)
        {
            if (field.AllowNull)
                return string.Empty;
            string originalDefaultValue = GetDefaultValueExpression(field.DbType);
            switch (field.DbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return StringUtilities.Format("'{0}'", StringUtilities.IsEmpty(field.DefaultValue) ? originalDefaultValue : EscapeSqlString(field.DefaultValue));
                case FieldDbType.AutoIncrement:
                    return string.Empty;
                default:
                    return StringUtilities.IsEmpty(field.DefaultValue) ? originalDefaultValue : field.DefaultValue;
            }
        }

        /// <summary>
        /// Generates a full column definition fragment (name + type + nullability + optional inline DEFAULT).
        /// Use <see cref="GetAutoIncrementColumnDefinition(DbField)"/> for AutoIncrement columns instead;
        /// SQLite requires inlining the primary key keyword on the same line.
        /// </summary>
        /// <remarks>
        /// Text columns (<see cref="FieldDbType.String"/>, <see cref="FieldDbType.Text"/>) are
        /// emitted with <c>COLLATE NOCASE</c> so string comparisons (<c>=</c>, <c>LIKE</c>,
        /// <c>ORDER BY</c>) are case-insensitive by default — matches the ERP expectation that
        /// <c>WHERE name = 'jeff'</c> hits a row stored as <c>Jeff</c>. Note SQLite's
        /// <c>NOCASE</c> only folds ASCII A–Z; non-ASCII characters remain case-sensitive.
        /// </remarks>
        /// <param name="field">The field definition.</param>
        public static string GetColumnDefinition(DbField field)
        {
            string dbType = SqliteTypeMapping.GetSqliteType(field);
            string collateClause = IsTextType(field.DbType) ? " COLLATE NOCASE" : string.Empty;
            string nullability = field.AllowNull ? "NULL" : "NOT NULL";
            string defaultExpression = GetDefaultExpression(field);
            string defaultClause = StringUtilities.IsNotEmpty(defaultExpression) ? $" DEFAULT {defaultExpression}" : string.Empty;
            return $"{QuoteName(field.FieldName)} {dbType}{collateClause} {nullability}{defaultClause}";
        }

        /// <summary>
        /// Returns true if the field stores text and should use <c>COLLATE NOCASE</c> for
        /// case-insensitive comparison.
        /// </summary>
        /// <param name="dbType">The field data type.</param>
        private static bool IsTextType(FieldDbType dbType)
        {
            return dbType == FieldDbType.String || dbType == FieldDbType.Text;
        }

        /// <summary>
        /// Generates the inline SQLite-specific column definition for an AutoIncrement primary key:
        /// <c>"name" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL</c>. SQLite cannot attach
        /// <c>AUTOINCREMENT</c> via an external PRIMARY KEY constraint.
        /// </summary>
        /// <param name="field">The AutoIncrement field definition.</param>
        public static string GetAutoIncrementColumnDefinition(DbField field)
        {
            return $"{QuoteName(field.FieldName)} INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL";
        }
    }
}
