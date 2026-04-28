using Bee.Base.Data;

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
    }
}
