using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Base.Data;
using Bee.Db.Ddl;
using Bee.Definition.Database;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// Generates CREATE TABLE SQL statements for MySQL 8.0+. Counterpart to
    /// <see cref="Sqlite.SqliteCreateTableCommandBuilder"/> and
    /// <see cref="PostgreSql.PgCreateTableCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dialect-specific output (see docs/plans/plan-mysql-support.md):
    /// </para>
    /// <list type="bullet">
    /// <item>Backtick-quoted identifiers (assumes <c>SQL_MODE</c> without <c>ANSI_QUOTES</c>).</item>
    /// <item><c>BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY</c> inlined on the AutoIncrement column line —
    ///       MySQL requires an AUTO_INCREMENT column to be the first column of an index, and the simplest
    ///       safe form is to make it the single-column primary key.</item>
    /// <item>Table suffix
    ///       <c>ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci</c> — InnoDB for ACID,
    ///       and the day-1 case-insensitive baseline collation so <c>WHERE name = 'jeff'</c> hits a row
    ///       stored as <c>'Jeff'</c> without builder-side <c>LOWER()</c> rewrites.</item>
    /// </list>
    /// </remarks>
    public class MySqlCreateTableCommandBuilder : ICreateTableCommandBuilder
    {
        private const string BaseTableSuffix =
            " ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci";

        private TableSchema? _dbTable;

        /// <summary>
        /// Initializes a new instance of <see cref="MySqlCreateTableCommandBuilder"/>.
        /// </summary>
        public MySqlCreateTableCommandBuilder()
        { }

        /// <summary>
        /// Gets the table schema definition.
        /// </summary>
        private TableSchema TableSchema => _dbTable!;

        /// <summary>
        /// Gets the SQL statement for creating a table.
        /// </summary>
        /// <param name="tableSchema">The table schema definition.</param>
        public string GetCommandText(TableSchema tableSchema)
        {
            _dbTable = tableSchema;
            ValidateAutoIncrement();
            return $"-- Create table {this.TableSchema.TableName}\r\n{this.GetCreateTableCommandText()}";
        }

        /// <summary>
        /// Gets the AutoIncrement field if the schema contains exactly one; null otherwise.
        /// Throws when multiple AutoIncrement fields are present (MySQL allows at most one
        /// <c>AUTO_INCREMENT</c> column per table).
        /// </summary>
        private DbField? GetAutoIncrementField()
        {
            var autoIncrementFields = this.TableSchema.Fields!
                .Where(f => f.DbType == FieldDbType.AutoIncrement)
                .ToList();

            if (autoIncrementFields.Count == 0) return null;
            if (autoIncrementFields.Count > 1)
                throw new InvalidOperationException(
                    $"On MySQL, table '{TableSchema.TableName}' declares {autoIncrementFields.Count} AutoIncrement fields. "
                    + "MySQL supports at most one AUTO_INCREMENT column per table.");

            return autoIncrementFields[0];
        }

        /// <summary>
        /// Validates that the AutoIncrement field, if present and a primary key is declared,
        /// is the single-column primary key. The inlined
        /// <c>BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY</c> form requires the AutoIncrement
        /// column to also be the table's PK; mismatch with an externally declared PK is
        /// rejected here to surface the schema bug at build time.
        /// </summary>
        private void ValidateAutoIncrement()
        {
            var autoIncrementField = GetAutoIncrementField();
            if (autoIncrementField == null) return;

            var primaryKey = this.TableSchema.GetPrimaryKey();
            if (primaryKey == null) return;

            if (primaryKey.IndexFields!.Count != 1
                || !StrFunc.IsEquals(primaryKey.IndexFields[0].FieldName, autoIncrementField.FieldName))
            {
                throw new InvalidOperationException(
                    $"On MySQL, AutoIncrement field '{autoIncrementField.FieldName}' must be the single-column primary key. "
                    + "Either restructure the schema or use FieldDbType.Long instead.");
            }
        }

        /// <summary>
        /// Gets the CREATE TABLE statement plus trailing CREATE INDEX statements.
        /// </summary>
        private string GetCreateTableCommandText()
        {
            string tableName = this.TableSchema.TableName;
            var autoIncrementField = GetAutoIncrementField();
            string fields = GetFieldsCommandText(autoIncrementField);
            string primaryKey = autoIncrementField == null
                ? GetPrimaryKeyCommandText(tableName)
                : string.Empty;
            string indexes = GetIndexesCommandText(tableName);

            var sb = new StringBuilder();
            sb.Append(CultureInfo.InvariantCulture, $"CREATE TABLE {MySqlSchemaSyntax.QuoteName(tableName)} (\r\n{fields}");
            if (StrFunc.IsNotEmpty(primaryKey))
                sb.Append(CultureInfo.InvariantCulture, $",\r\n  {primaryKey}");
            sb.Append(CultureInfo.InvariantCulture, $"\r\n){GetTableSuffix()};");
            if (StrFunc.IsNotEmpty(indexes))
                sb.Append(CultureInfo.InvariantCulture, $"\r\n{indexes}");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the table suffix, appending <c>COMMENT='...'</c> when the schema has a
        /// non-empty <see cref="TableSchema.DisplayName"/>. The schema reader round-trips
        /// the table COMMENT, so emitting it here keeps fixture re-runs idempotent (no
        /// phantom description drift).
        /// </summary>
        private string GetTableSuffix()
        {
            if (StrFunc.IsEmpty(TableSchema.DisplayName))
                return BaseTableSuffix;
            return $"{BaseTableSuffix} COMMENT='{MySqlSchemaSyntax.EscapeSqlString(TableSchema.DisplayName)}'";
        }

        /// <summary>
        /// Gets the SQL fragment for all column definitions. The AutoIncrement column is
        /// emitted with the inlined MySQL form
        /// <c>`name` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY</c>.
        /// </summary>
        private string GetFieldsCommandText(DbField? autoIncrementField)
        {
            var sb = new StringBuilder();
            foreach (DbField field in this.TableSchema.Fields!)
            {
                if (sb.Length > 0)
                    sb.Append(",\r\n");
                if (autoIncrementField != null && field == autoIncrementField)
                    sb.Append("  " + MySqlSchemaSyntax.GetAutoIncrementColumnDefinition(field));
                else
                    sb.Append("  " + MySqlSchemaSyntax.GetColumnDefinition(field));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the inline PRIMARY KEY constraint SQL fragment. Returns empty when an
        /// AutoIncrement column is present (PK is inlined on the column).
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private string GetPrimaryKeyCommandText(string tableName)
        {
            var index = this.TableSchema.GetPrimaryKey();
            if (index == null) return string.Empty;

            var fieldBuilder = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (fieldBuilder.Length > 0)
                    fieldBuilder.Append(", ");
                fieldBuilder.Append(CultureInfo.InvariantCulture,
                    $"{MySqlSchemaSyntax.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }

            string name = StrFunc.Format(index.Name, tableName);
            return $"CONSTRAINT {MySqlSchemaSyntax.QuoteName(name)} PRIMARY KEY ({fieldBuilder})";
        }

        /// <summary>
        /// Gets the SQL statements for creating all non-primary-key indexes.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private string GetIndexesCommandText(string tableName)
        {
            var sb = new StringBuilder();
            foreach (TableSchemaIndex index in this.TableSchema.Indexes!.Where(i => !i.PrimaryKey))
            {
                sb.AppendLine(GetIndexCommandText(tableName, index));
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Gets the SQL statement for creating a single index.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="index">The table schema index definition.</param>
        private static string GetIndexCommandText(string tableName, TableSchemaIndex index)
        {
            string name = StrFunc.Format(index.Name, tableName);
            var fieldBuilder = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (fieldBuilder.Length > 0)
                    fieldBuilder.Append(", ");
                fieldBuilder.Append(CultureInfo.InvariantCulture,
                    $"{MySqlSchemaSyntax.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }

            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            return $"CREATE {uniqueClause}INDEX {MySqlSchemaSyntax.QuoteName(name)} ON {MySqlSchemaSyntax.QuoteName(tableName)} ({fieldBuilder});";
        }
    }
}
