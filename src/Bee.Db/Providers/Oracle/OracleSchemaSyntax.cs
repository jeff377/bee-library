using Bee.Base;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Db.Providers.Oracle
{
    /// <summary>
    /// Shared Oracle identifier and column-definition primitives used by the CREATE and
    /// ALTER schema builders. Counterpart to <see cref="MySql.MySqlSchemaSyntax"/> and
    /// <see cref="PostgreSql.PgSchemaSyntax"/>.
    /// </summary>
    /// <remarks>
    /// Targets Oracle 19c+. All identifiers are quoted with double quotes — this is required
    /// because Oracle has a wide reserved-word set (e.g. <c>COMMENT</c>, <c>SIZE</c>,
    /// <c>LEVEL</c>, <c>SESSION</c>); quoted identifiers also become case-sensitive, so the
    /// FormSchema convention is to use lowercase names. See docs/plans/plan-oracle-support.md.
    /// </remarks>
    internal static class OracleSchemaSyntax
    {
        /// <summary>
        /// Quotes an Oracle identifier with double quotes; embedded double quotes are doubled.
        /// </summary>
        /// <param name="identifier">The identifier to quote.</param>
        public static string QuoteName(string identifier)
        {
            return $"\"{identifier.ToUpperInvariant().Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// Gets the Oracle built-in default value expression for the specified field type.
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
                    return "SYSTIMESTAMP";
                case FieldDbType.Guid:
                    return "SYS_GUID()";
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

            // Oracle treats '' as NULL, so a String column cannot hold a non-null empty string:
            // `DEFAULT ''` would silently mean `DEFAULT NULL` and contradict NOT NULL. String
            // columns are therefore emitted nullable (see GetColumnDefinition) and the empty-string
            // default is dropped; the framework's "text columns are never null" guarantee is upheld
            // at the C# layer via `ValueUtilities.CStr(null)` returning an empty string. An explicit
            // non-empty default is still a valid non-null literal on a nullable column, so it is
            // preserved to keep the read-back schema diff stable.
            // See docs/plans/plan-oracle-string-nullability.md.
            if (field.DbType == FieldDbType.String)
            {
                return StringUtilities.IsEmpty(field.DefaultValue)
                    ? string.Empty
                    : StringUtilities.Format("'{0}'", EscapeSqlString(field.DefaultValue));
            }

            // CLOB / BLOB reject an inline literal `DEFAULT` in the framework's `CREATE TABLE` shape.
            // Text is additionally emitted nullable for the same `''`-is-`NULL` reason as String.
            if (field.DbType == FieldDbType.Text || field.DbType == FieldDbType.Binary)
                return string.Empty;

            string originalDefaultValue = GetDefaultValueExpression(field.DbType);
            switch (field.DbType)
            {
                case FieldDbType.AutoIncrement:
                    return string.Empty;
                default:
                    return StringUtilities.IsEmpty(field.DefaultValue) ? originalDefaultValue : field.DefaultValue;
            }
        }

        /// <summary>
        /// Generates a full column definition fragment (name + type + optional inline DEFAULT + nullability).
        /// Use <see cref="GetAutoIncrementColumnDefinition(DbField)"/> for AutoIncrement columns instead;
        /// Oracle requires the <c>GENERATED BY DEFAULT AS IDENTITY</c> clause to follow the type
        /// directly with no DEFAULT.
        /// </summary>
        /// <remarks>
        /// Oracle column-clause order is <c>type [DEFAULT expr] [NOT NULL]</c>, which is
        /// the inverse of MySQL/PG (<c>type [NOT NULL] [DEFAULT expr]</c>). Captions are not
        /// emitted inline: Oracle stores column comments in a separate <c>COMMENT ON COLUMN</c>
        /// statement which the CREATE TABLE builder appends after the table is created.
        /// </remarks>
        /// <param name="field">The field definition.</param>
        public static string GetColumnDefinition(DbField field)
        {
            string dbType = OracleTypeMapping.GetOracleType(field);
            // String/Text columns are always emitted nullable regardless of the definition's
            // AllowNull: Oracle equates '' with NULL, so "non-null empty string" is inexpressible.
            // See docs/plans/plan-oracle-string-nullability.md.
            bool isNullableText = field.DbType == FieldDbType.String || field.DbType == FieldDbType.Text;
            string nullability = (field.AllowNull || isNullableText) ? "NULL" : "NOT NULL";
            string defaultExpression = GetDefaultExpression(field);
            string defaultClause = StringUtilities.IsNotEmpty(defaultExpression) ? $" DEFAULT {defaultExpression}" : string.Empty;
            return $"{QuoteName(field.FieldName)} {dbType}{defaultClause} {nullability}";
        }

        /// <summary>
        /// Generates the inline Oracle-specific column definition for an AutoIncrement primary key:
        /// <c>"name" NUMBER(19) GENERATED BY DEFAULT AS IDENTITY NOT NULL</c>. The <c>PRIMARY KEY</c>
        /// constraint is emitted as a separate table-level constraint by the CREATE TABLE builder.
        /// </summary>
        /// <param name="field">The AutoIncrement field definition.</param>
        public static string GetAutoIncrementColumnDefinition(DbField field)
        {
            return $"{QuoteName(field.FieldName)} NUMBER(19) GENERATED BY DEFAULT AS IDENTITY NOT NULL";
        }

        /// <summary>
        /// Generates the <c>COMMENT ON COLUMN "table"."column" IS 'caption'</c> statement for
        /// a column with a non-empty caption, or empty string when the caption is empty.
        /// Oracle stores column captions out-of-band in <c>ALL_COL_COMMENTS</c>, so this is
        /// emitted as a follow-up statement after CREATE TABLE.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="field">The field definition.</param>
        public static string GetCommentStatement(string tableName, DbField field)
        {
            if (StringUtilities.IsEmpty(field.Caption))
                return string.Empty;

            return $"COMMENT ON COLUMN {QuoteName(tableName)}.{QuoteName(field.FieldName)} IS '{EscapeSqlString(field.Caption)}'";
        }
    }
}
