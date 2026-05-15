using Bee.Base;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Db.Providers.PostgreSql
{
    /// <summary>
    /// Shared PostgreSQL identifier, literal, and column-definition primitives used by
    /// the CREATE and ALTER schema builders. Counterpart to
    /// <see cref="SqlServer.SqlSchemaSyntax"/> for the SQL Server provider.
    /// </summary>
    internal static class PgSchemaSyntax
    {
        /// <summary>
        /// Quotes a PostgreSQL identifier by escaping <c>"</c> as <c>""</c> and wrapping in double quotes.
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
        /// Gets the PostgreSQL built-in default value expression for the specified field type
        /// (e.g. <c>CURRENT_TIMESTAMP</c> for DateTime, <c>gen_random_uuid()</c> for Guid,
        /// <c>0</c> for numeric).
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
                    return "gen_random_uuid()";
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
                case FieldDbType.Boolean:
                {
                    // PG `BOOLEAN` only accepts `TRUE`/`FALSE` literals, not the integer `1`/`0`
                    // that other dialects accept. The framework keeps `"1"`/`"0"` as the canonical
                    // user-facing form; PG translates here at the SQL emission boundary.
                    string raw = StringUtilities.IsEmpty(field.DefaultValue) ? originalDefaultValue : field.DefaultValue;
                    return StringUtilities.IsEquals(raw, "1") ? "TRUE" : "FALSE";
                }
                default:
                    return StringUtilities.IsEmpty(field.DefaultValue) ? originalDefaultValue : field.DefaultValue;
            }
        }

        /// <summary>
        /// Generates a full column definition fragment (name + type + nullability + optional inline DEFAULT).
        /// </summary>
        /// <param name="field">The field definition.</param>
        public static string GetColumnDefinition(DbField field)
        {
            string dbType = PgTypeMapping.GetPgType(field);
            string nullability = field.AllowNull ? "NULL" : "NOT NULL";
            string defaultExpression = GetDefaultExpression(field);
            string defaultClause = StringUtilities.IsNotEmpty(defaultExpression) ? $" DEFAULT {defaultExpression}" : string.Empty;
            return $"{QuoteName(field.FieldName)} {dbType} {nullability}{defaultClause}";
        }
    }
}
