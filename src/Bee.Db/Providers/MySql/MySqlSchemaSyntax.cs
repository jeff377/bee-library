using Bee.Base;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// Shared MySQL identifier and column-definition primitives used by the CREATE and
    /// ALTER schema builders. Counterpart to <see cref="Sqlite.SqliteSchemaSyntax"/> and
    /// <see cref="PostgreSql.PgSchemaSyntax"/>.
    /// </summary>
    /// <remarks>
    /// Targets MySQL 8.0+. Assumes the server's <c>SQL_MODE</c> does not include
    /// <c>ANSI_QUOTES</c>; backtick quoting and <c>utf8mb4_0900_ai_ci</c> collation
    /// (which is accent- and case-insensitive) are 8.0 defaults. See
    /// <c>docs/database-dialect-differences.md</c> §4.
    /// </remarks>
    internal static class MySqlSchemaSyntax
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
                case FieldDbType.Time:
                    return string.Empty;
                case FieldDbType.Boolean:
                case FieldDbType.Short:
                case FieldDbType.Integer:
                case FieldDbType.Long:
                case FieldDbType.Decimal:
                case FieldDbType.Currency:
                    return "0";
                case FieldDbType.Date:
                    // Parenthesised because MySQL 8.0.13+ requires the expression form for any
                    // non-literal default other than `CURRENT_TIMESTAMP`. The inner `()` matters
                    // too: INFORMATION_SCHEMA reports the stored default as `utc_date()`, and the
                    // schema comparison strips one layer of outer parentheses before comparing —
                    // so emitting `(UTC_DATE)` would never match what is read back, and every
                    // schema check would re-emit the same ALTER.
                    return "(UTC_DATE())";
                case FieldDbType.DateTime:
                    // The framework stores every instant in UTC (ADR-032 D1), and this DEFAULT is
                    // the path that writes when a hand-written INSERT omits the column or an
                    // ALTER TABLE ADD COLUMN backfills existing rows. The server's local clock
                    // would put a non-UTC value into a column defined as UTC, so the UTC-returning
                    // form is used. (This is about the storage basis, not the user's zone — the
                    // database has no session and cannot know the user; user-facing defaults come
                    // from `FormRowDefaults`, see D12.)
                    // Parenthesised: MySQL 8.0.13+ accepts a bare function as a DEFAULT only for
                    // `CURRENT_TIMESTAMP`; any other expression — `UTC_TIMESTAMP(6)` included — is a
                    // syntax error unless wrapped, the same rule the `DATE` case above already hits.
                    return "(UTC_TIMESTAMP(6))";
                case FieldDbType.Guid:
                    return "(UUID())";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Escapes a string value for use inside a <c>'...'</c> literal.
        /// </summary>
        /// <param name="value">The string value to escape.</param>
        /// <remarks>
        /// <para>
        /// WARNING: MySQL is the one dialect here where doubling the quote is not enough. Unlike SQL
        /// Server, Oracle, SQLite and PostgreSQL under its default <c>standard_conforming_strings</c>,
        /// MySQL treats <c>\</c> as an escape character inside a string literal. Doubling only the
        /// quote leaves the backslash live, so it consumes the quote that follows it and the literal
        /// runs on into whatever comes next.
        /// </para>
        /// <para>
        /// This is not only an injection surface. A caption or description that merely <b>ends with a
        /// backslash</b> — a Windows path in a field comment, say — breaks the generated DDL outright.
        /// Measured against MySQL 8: <c>ends with backslash \</c> produced a syntax error, and
        /// <c>a\' , (SELECT 1) , '</c> escaped the literal entirely.
        /// </para>
        /// <para>
        /// The backslash is replaced first. With only these two replacements the order happens not to
        /// matter, but escaping the escape character before anything else is the ordering that stays
        /// correct if another sequence is ever added.
        /// </para>
        /// <para>
        /// IMPORTANT: this assumes the server's default <c>sql_mode</c>, which does not include
        /// <c>NO_BACKSLASH_ESCAPES</c>. A deployment that enables that mode makes the backslash an
        /// ordinary character, and comments and default values written through here would carry
        /// doubled backslashes. The function has no connection and so cannot read the mode; the same
        /// assumption is already made by the PostgreSQL dialect for <c>standard_conforming_strings</c>.
        /// </para>
        /// </remarks>
        public static string EscapeSqlString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("'", "''");
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

            // MySQL 8.0: BLOB/TEXT/JSON columns reject inline string-literal DEFAULT
            // (only parenthesised expression defaults are allowed). Suppress DEFAULT for
            // Text columns — they remain NOT NULL but the caller must provide a value on
            // INSERT. Binary maps to LONGBLOB and has no default in the framework either.
            if (field.DbType == FieldDbType.Text)
                return string.Empty;

            string originalDefaultValue = GetDefaultValueExpression(field.DbType);
            switch (field.DbType)
            {
                case FieldDbType.String:
                case FieldDbType.Time:
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
        /// MySQL requires inlining <c>AUTO_INCREMENT PRIMARY KEY</c> on the same line.
        /// </summary>
        /// <remarks>
        /// Case-insensitive comparison is provided table-wide via the
        /// <c>COLLATE=utf8mb4_0900_ai_ci</c> table-level clause emitted by the CREATE
        /// TABLE builder, so column-level <c>COLLATE</c> is not required here (MySQL
        /// columns inherit the table collation by default).
        /// </remarks>
        /// <param name="field">The field definition.</param>
        /// <param name="defaultOverride">
        /// When supplied, replaces the field's resolved DEFAULT clause (already formatted, e.g. a
        /// quoted literal). Used by the ALTER ADD path to seed a non-deterministic default (UUID())
        /// with a replication-safe constant before restoring the real default separately.
        /// </param>
        public static string GetColumnDefinition(DbField field, string? defaultOverride = null)
        {
            string dbType = MySqlTypeMapping.GetMySqlType(field);
            string nullability = field.AllowNull ? "NULL" : "NOT NULL";
            string defaultExpression = defaultOverride ?? GetDefaultExpression(field);
            string defaultClause = StringUtilities.IsNotEmpty(defaultExpression) ? $" DEFAULT {defaultExpression}" : string.Empty;
            string commentClause = GetCommentClause(field.Caption);
            return $"{QuoteName(field.FieldName)} {dbType} {nullability}{defaultClause}{commentClause}";
        }

        /// <summary>
        /// Generates the inline MySQL-specific column definition for an AutoIncrement primary key:
        /// <c>`name` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY</c>.
        /// </summary>
        /// <param name="field">The AutoIncrement field definition.</param>
        public static string GetAutoIncrementColumnDefinition(DbField field)
        {
            string commentClause = GetCommentClause(field.Caption);
            return $"{QuoteName(field.FieldName)} BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY{commentClause}";
        }

        /// <summary>
        /// Generates the column definition for an <c>ALTER TABLE ... MODIFY COLUMN</c> on an existing
        /// column.
        /// </summary>
        /// <remarks>
        /// WARNING: MySQL's MODIFY replaces the whole column definition, so anything the fragment
        /// omits is dropped — <see cref="GetColumnDefinition"/> carries no <c>AUTO_INCREMENT</c>, and
        /// using it on an identity column silently turns that column into a plain BIGINT with no
        /// default (every later INSERT then fails with "Field 'x' doesn't have a default value").
        /// The AutoIncrement form is emitted without the inline <c>PRIMARY KEY</c> clause that
        /// <see cref="GetAutoIncrementColumnDefinition"/> carries, since the table already has one.
        /// </remarks>
        /// <param name="field">The field definition.</param>
        public static string GetModifyColumnDefinition(DbField field)
        {
            if (field.DbType != FieldDbType.AutoIncrement)
                return GetColumnDefinition(field);

            return $"{QuoteName(field.FieldName)} BIGINT NOT NULL AUTO_INCREMENT{GetCommentClause(field.Caption)}";
        }

        /// <summary>
        /// Returns the <c>COMMENT 'caption'</c> clause for a column with a non-empty caption,
        /// or empty string when the caption is empty. The framework emits COMMENT so the
        /// schema reader can round-trip captions cleanly (otherwise every fixture re-run
        /// would detect a description drift on every text column).
        /// </summary>
        private static string GetCommentClause(string caption)
        {
            return StringUtilities.IsEmpty(caption) ? string.Empty : $" COMMENT '{EscapeSqlString(caption)}'";
        }
    }
}
