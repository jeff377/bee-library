using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Db.Ddl;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// Generates CREATE TABLE SQL statements for SQLite. Counterpart to
    /// <see cref="PostgreSql.PgCreateTableCommandBuilder"/> with SQLite-specific dialect:
    /// double-quoted identifiers, <c>INTEGER PRIMARY KEY AUTOINCREMENT</c> inlined on the
    /// AutoIncrement column line, and no <c>COMMENT ON</c> output (SQLite does not persist
    /// table or column descriptions).
    /// </summary>
    public class SqliteCreateTableCommandBuilder : ICreateTableCommandBuilder
    {
        private TableSchema? _dbTable;

        /// <summary>
        /// Initializes a new instance of <see cref="SqliteCreateTableCommandBuilder"/>.
        /// </summary>
        public SqliteCreateTableCommandBuilder()
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
        /// Throws when multiple AutoIncrement fields are present, or when the AutoIncrement
        /// field is not the single-column primary key (SQLite cannot attach AUTOINCREMENT
        /// via an external PRIMARY KEY constraint).
        /// </summary>
        private DbField? GetAutoIncrementField()
        {
            var autoIncrementFields = this.TableSchema.Fields!
                .Where(f => f.DbType == FieldDbType.AutoIncrement)
                .ToList();

            if (autoIncrementFields.Count == 0) return null;
            if (autoIncrementFields.Count > 1)
                throw new InvalidOperationException(
                    $"On SQLite, table '{TableSchema.TableName}' declares {autoIncrementFields.Count} AutoIncrement fields. "
                    + "SQLite supports at most one AUTOINCREMENT column.");

            return autoIncrementFields[0];
        }

        /// <summary>
        /// Validates the SQLite-specific constraint that an AutoIncrement field, if a primary
        /// key is declared in the schema, must be the single-column primary key. When the
        /// schema does not declare a primary key, the inlined
        /// <c>INTEGER PRIMARY KEY AUTOINCREMENT</c> is the table's PK by definition and
        /// validation is skipped.
        /// </summary>
        private void ValidateAutoIncrement()
        {
            var autoIncrementField = GetAutoIncrementField();
            if (autoIncrementField == null) return;

            var primaryKey = this.TableSchema.GetPrimaryKey();
            if (primaryKey == null) return;

            if (primaryKey.IndexFields!.Count != 1
                || !StringUtilities.IsEquals(primaryKey.IndexFields[0].FieldName, autoIncrementField.FieldName))
            {
                throw new InvalidOperationException(
                    $"On SQLite, AutoIncrement field '{autoIncrementField.FieldName}' must be the single-column primary key. "
                    + "Either restructure the schema or use FieldDbType.Integer instead.");
            }
        }

        /// <summary>
        /// Gets the CREATE TABLE SQL statement plus trailing CREATE INDEX statements.
        /// SQLite does not persist descriptions, so no COMMENT statements are produced.
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
            sb.Append(CultureInfo.InvariantCulture, $"CREATE TABLE {SqliteSchemaSyntax.QuoteName(tableName)} (\r\n{fields}");
            if (StringUtilities.IsNotEmpty(primaryKey))
                sb.Append(CultureInfo.InvariantCulture, $",\r\n  {primaryKey}");
            sb.Append("\r\n);");
            if (StringUtilities.IsNotEmpty(indexes))
                sb.Append(CultureInfo.InvariantCulture, $"\r\n{indexes}");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the SQL fragment for all column definitions. The AutoIncrement column is
        /// emitted with the inlined SQLite primary-key form
        /// <c>"name" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL</c>.
        /// </summary>
        private string GetFieldsCommandText(DbField? autoIncrementField)
        {
            var sb = new StringBuilder();
            foreach (DbField field in this.TableSchema.Fields!)
            {
                if (sb.Length > 0)
                    sb.Append(",\r\n");
                if (autoIncrementField != null && field == autoIncrementField)
                    sb.Append("  " + SqliteSchemaSyntax.GetAutoIncrementColumnDefinition(field));
                else
                    sb.Append("  " + SqliteSchemaSyntax.GetColumnDefinition(field));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the inline PRIMARY KEY constraint SQL fragment. Returns empty when an
        /// AutoIncrement column is present (in which case the PK is inlined on the column).
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
                    $"{SqliteSchemaSyntax.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }

            string name = StringUtilities.Format(index.Name, tableName);
            return $"CONSTRAINT {SqliteSchemaSyntax.QuoteName(name)} PRIMARY KEY ({fieldBuilder})";
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
            string name = StringUtilities.Format(index.Name, tableName);
            var fieldBuilder = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (fieldBuilder.Length > 0)
                    fieldBuilder.Append(", ");
                fieldBuilder.Append(CultureInfo.InvariantCulture,
                    $"{SqliteSchemaSyntax.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }

            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            return $"CREATE {uniqueClause}INDEX {SqliteSchemaSyntax.QuoteName(name)} ON {SqliteSchemaSyntax.QuoteName(tableName)} ({fieldBuilder});";
        }
    }
}
