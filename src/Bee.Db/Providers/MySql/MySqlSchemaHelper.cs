using Bee.Base;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// Shared MySQL identifier and column-definition primitives used by the CREATE and
    /// ALTER schema builders. Counterpart to <see cref="Sqlite.SqliteSchemaHelper"/> and
    /// <see cref="PostgreSql.PgSchemaHelper"/>.
    /// </summary>
    /// <remarks>
    /// Targets MySQL 8.0+. Assumes the server's <c>SQL_MODE</c> does not include
    /// <c>ANSI_QUOTES</c>; backtick quoting and <c>utf8mb4_0900_ai_ci</c> collation
    /// (which is accent- and case-insensitive) are 8.0 defaults — see
    /// docs/plans/plan-mysql-support.md.
    /// </remarks>
    internal static class MySqlSchemaHelper
    {
        /// <summary>
        /// Quotes a MySQL identifier with backticks; embedded backticks are doubled.
        /// </summary>
        /// <param name="identifier">The identifier to quote.</param>
        public static string QuoteName(string identifier)
        {
            return $"`{identifier.Replace("`", "``")}`";
        }

        /// <summary>
        /// Gets the MySQL built-in default value expression for the specified field type.
        /// <c>UUID()</c> is wrapped in parentheses because MySQL only allows
        /// function-call default values inside an expression default (parenthesized form).
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
                    return "CURRENT_TIMESTAMP(6)";
                case FieldDbType.Guid:
                    return "(UUID())";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Escapes a string value for use inside a <c>'...'</c> literal by doubling single quotes.
        /// </summary>
        /// <param name="value">The string value to escape.</param>
        public static string EscapeSqlString(string value)
        {
            return value.Replace("'", "''");
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
                    return StrFunc.Format("'{0}'", StrFunc.IsEmpty(field.DefaultValue) ? originalDefaultValue : EscapeSqlString(field.DefaultValue));
                case FieldDbType.AutoIncrement:
                    return string.Empty;
                default:
                    return StrFunc.IsEmpty(field.DefaultValue) ? originalDefaultValue : field.DefaultValue;
            }
        }

        /// <summary>
        /// Generates a full column definition fragment (name + type + nullability + optional inline DEFAULT).
        /// Use <see cref="GetAutoIncrementColumnDefinition(DbField)"/> for AutoIncrement columns instead;
        /// MySQL requires inlining <c>AUTO_INCREMENT PRIMARY KEY</c> on the same line.
        /// </summary>
        /// <remarks>
        /// Case-insensitive comparison is provided table-wide via the
        /// <c>COLLATE=utf8mb4_0900_ai_ci</c> table-level clause emitted by the CREATE
        /// TABLE builder, so column-level <c>COLLATE</c> is not required here (MySQL
        /// columns inherit the table collation by default).
        /// </remarks>
        /// <param name="field">The field definition.</param>
        public static string GetColumnDefinition(DbField field)
        {
            string dbType = MySqlTypeMapping.GetMySqlType(field);
            string nullability = field.AllowNull ? "NULL" : "NOT NULL";
            string defaultExpression = GetDefaultExpression(field);
            string defaultClause = StrFunc.IsNotEmpty(defaultExpression) ? $" DEFAULT {defaultExpression}" : string.Empty;
            return $"{QuoteName(field.FieldName)} {dbType} {nullability}{defaultClause}";
        }

        /// <summary>
        /// Generates the inline MySQL-specific column definition for an AutoIncrement primary key:
        /// <c>`name` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY</c>.
        /// </summary>
        /// <param name="field">The AutoIncrement field definition.</param>
        public static string GetAutoIncrementColumnDefinition(DbField field)
        {
            return $"{QuoteName(field.FieldName)} BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY";
        }
    }
}
