using Bee.Definition.Database;
using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Db.Ddl;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Generates CREATE TABLE SQL statements for SQL Server.
    /// </summary>
    public class SqlCreateTableCommandBuilder : ICreateTableCommandBuilder
    {
        private TableSchema? _dbTable = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="SqlCreateTableCommandBuilder"/>.
        /// </summary>
        public SqlCreateTableCommandBuilder()
        { }

        #endregion

        /// <summary>
        /// Gets the table schema definition.
        /// </summary>
        private TableSchema TableSchema
        {
            get { return _dbTable!; }
        }

        /// <summary>
        /// Gets the table name.
        /// </summary>
        private string TableName
        {
            get { return this.TableSchema.TableName; }
        }

        /// <summary>
        /// Gets the SQL statement for creating a table.
        /// </summary>
        /// <param name="tableSchema">The table schema definition.</param>
        public string GetCommandText(TableSchema tableSchema)
        {
            _dbTable = tableSchema;
            return $"-- Create table {this.TableName}\r\n{this.GetCreateTableCommandText()}";
        }

        /// <summary>
        /// Gets the CREATE TABLE SQL statement.
        /// </summary>
        /// <param name="tableName">The table name; uses the schema table name if empty.</param>
        private string GetCreateTableCommandText(string tableName = "")
        {
            // Table name
            string dbTableName = StringUtilities.IsNotEmpty(tableName) ? tableName : this.TableSchema.TableName;
            // Build the column definitions clause
            string fields = GetFieldsCommandText();
            // Build the primary key constraint clause
            string primaryKey = GetPrimaryKeyCommandText(dbTableName);
            // Build the index creation clause
            string indexs = GetIndexsCommandText(dbTableName);

            var sb = new StringBuilder();
            // Assemble the CREATE TABLE statement
            sb.Append(CultureInfo.InvariantCulture, $"CREATE TABLE {SqlSchemaSyntax.QuoteName(dbTableName)} (\r\n{fields}");
            if (StringUtilities.IsNotEmpty(primaryKey))
                sb.Append(CultureInfo.InvariantCulture, $",\r\n  {primaryKey}");
            sb.Append("\r\n);");
            // Append the index creation statements
            if (StringUtilities.IsNotEmpty(indexs))
                sb.Append(CultureInfo.InvariantCulture, $"\r\n{indexs}");
            // Append extended property statements for table and column descriptions
            string extendedProperty = GetExtendedPropertyCommandText(dbTableName);
            if (StringUtilities.IsNotEmpty(extendedProperty))
                sb.Append(CultureInfo.InvariantCulture, $"\r\n{extendedProperty}");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the sp_addextendedproperty SQL fragment for table and column descriptions.
        /// </summary>
        /// <param name="dbTableName">The target table name (tmp or final).</param>
        private string GetExtendedPropertyCommandText(string dbTableName)
        {
            var sb = new StringBuilder();
            // Table-level description sourced from DisplayName
            if (StringUtilities.IsNotEmpty(this.TableSchema.DisplayName))
                sb.AppendLine(GetAddTableExtendedPropertyCommand(dbTableName, this.TableSchema.DisplayName));
            // Column-level descriptions sourced from Caption
            foreach (var field in this.TableSchema.Fields!.Where(f => StringUtilities.IsNotEmpty(f.Caption)))
            {
                sb.AppendLine(GetAddColumnExtendedPropertyCommand(dbTableName, field.FieldName, field.Caption));
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Gets the sp_addextendedproperty SQL for a table-level description.
        /// </summary>
        /// <param name="tableName">The target table name.</param>
        /// <param name="description">The description text.</param>
        private static string GetAddTableExtendedPropertyCommand(string tableName, string description)
        {
            return $"EXEC sp_addextendedproperty\r\n" +
                   $"  @name=N'MS_Description', @value=N'{SqlSchemaSyntax.EscapeSqlString(description)}',\r\n" +
                   $"  @level0type=N'SCHEMA', @level0name=N'dbo',\r\n" +
                   $"  @level1type=N'TABLE', @level1name=N'{SqlSchemaSyntax.EscapeSqlString(tableName)}';";
        }

        /// <summary>
        /// Gets the sp_addextendedproperty SQL for a column-level description.
        /// </summary>
        /// <param name="tableName">The target table name.</param>
        /// <param name="columnName">The column name.</param>
        /// <param name="description">The description text.</param>
        private static string GetAddColumnExtendedPropertyCommand(string tableName, string columnName, string description)
        {
            return $"EXEC sp_addextendedproperty\r\n" +
                   $"  @name=N'MS_Description', @value=N'{SqlSchemaSyntax.EscapeSqlString(description)}',\r\n" +
                   $"  @level0type=N'SCHEMA', @level0name=N'dbo',\r\n" +
                   $"  @level1type=N'TABLE', @level1name=N'{SqlSchemaSyntax.EscapeSqlString(tableName)}',\r\n" +
                   $"  @level2type=N'COLUMN', @level2name=N'{SqlSchemaSyntax.EscapeSqlString(columnName)}';";
        }

        /// <summary>
        /// Gets the SQL fragment for all column definitions.
        /// </summary>
        private string GetFieldsCommandText()
        {
            // Build the column definitions
            var sb = new StringBuilder();
            foreach (DbField field in this.TableSchema.Fields!)
            {
                // Get the SQL fragment for this column
                string text = SqlSchemaSyntax.GetColumnDefinition(field);
                if (StringUtilities.IsNotEmpty(text))
                {
                    if (sb.Length > 0)
                        sb.Append(",\r\n");
                    sb.Append("  " + text);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the primary key constraint SQL fragment.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private string GetPrimaryKeyCommandText(string tableName)
        {
            var index = this.TableSchema.GetPrimaryKey();
            if (index == null) { return string.Empty; }

            // Build the index field list
            var fieldBuilder = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (fieldBuilder.Length > 0)
                    fieldBuilder.Append(", ");
                fieldBuilder.Append(CultureInfo.InvariantCulture, $"{SqlSchemaSyntax.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }

            string name = StringUtilities.Format(index.Name, tableName);
            return $"CONSTRAINT {SqlSchemaSyntax.QuoteName(name)} PRIMARY KEY ({fieldBuilder})";
        }

        /// <summary>
        /// Gets the SQL statements for creating all non-primary-key indexes.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private string GetIndexsCommandText(string tableName)
        {
            var sb = new StringBuilder();
            foreach (DbTableIndex index in this.TableSchema.Indexes!)
            {
                if (!index.PrimaryKey)
                    sb.AppendLine(GetIndexCommandText(tableName, index));
            }
            return sb.ToString().Trim(); // 避免最後多餘的換行
        }

        /// <summary>
        /// Gets the SQL statement for creating a single index.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="index">The table schema index definition.</param>
        private static string GetIndexCommandText(string tableName, DbTableIndex index)
        {
            // Index name
            string name = StringUtilities.Format(index.Name, tableName);
            // Index fields
            var fieldBuilder = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (fieldBuilder.Length > 0)
                    fieldBuilder.Append(", ");
                fieldBuilder.Append(CultureInfo.InvariantCulture, $"{SqlSchemaSyntax.QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }
            // Generate the CREATE INDEX statement
            if (index.Unique)
                return $"CREATE UNIQUE INDEX {SqlSchemaSyntax.QuoteName(name)} ON {SqlSchemaSyntax.QuoteName(tableName)} ({fieldBuilder});";
            else
                return $"CREATE INDEX {SqlSchemaSyntax.QuoteName(name)} ON {SqlSchemaSyntax.QuoteName(tableName)} ({fieldBuilder});";
        }
    }
}
