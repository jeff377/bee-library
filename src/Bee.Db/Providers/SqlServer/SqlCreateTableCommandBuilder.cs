using Bee.Definition.Database;
using System.Globalization;
using System.Text;
using Bee.Base;
using Bee.Base.Data;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Generates CREATE TABLE SQL statements for SQL Server.
    /// </summary>
    public class SqlCreateTableCommandBuilder : ICreateTableCommandBuilder
    {
        private TableSchema? _dbTable = null;

        #region 建構函式

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
        /// Quotes a SQL Server identifier by escaping <c>]</c> as <c>]]</c> and wrapping in square brackets.
        /// </summary>
        /// <param name="identifier">The identifier to quote.</param>
        private static string QuoteName(string identifier)
        {
            return $"[{identifier.Replace("]", "]]")}]";
        }

        /// <summary>
        /// Escapes a string value for use inside an N'...' literal by doubling single quotes.
        /// </summary>
        /// <param name="value">The string value to escape.</param>
        private static string EscapeSqlString(string value)
        {
            return value.Replace("'", "''");
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
            string dbTableName = StrFunc.IsNotEmpty(tableName) ? tableName : this.TableSchema.TableName;
            // Build the column definitions clause
            string fields = GetFieldsCommandText();
            // Build the primary key constraint clause
            string primaryKey = GetPrimaryKeyCommandText(dbTableName);
            // Build the index creation clause
            string indexs = GetIndexsCommandText(dbTableName);

            var sb = new StringBuilder();
            // Assemble the CREATE TABLE statement
            sb.Append(CultureInfo.InvariantCulture, $"CREATE TABLE {QuoteName(dbTableName)} (\r\n{fields}");
            if (StrFunc.IsNotEmpty(primaryKey))
                sb.Append(CultureInfo.InvariantCulture, $",\r\n  {primaryKey}");
            sb.Append("\r\n);");
            // Append the index creation statements
            if (StrFunc.IsNotEmpty(indexs))
                sb.Append(CultureInfo.InvariantCulture, $"\r\n{indexs}");
            // Append extended property statements for table and column descriptions
            string extendedProperty = GetExtendedPropertyCommandText(dbTableName);
            if (StrFunc.IsNotEmpty(extendedProperty))
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
            if (StrFunc.IsNotEmpty(this.TableSchema.DisplayName))
                sb.AppendLine(GetAddTableExtendedPropertyCommand(dbTableName, this.TableSchema.DisplayName));
            // Column-level descriptions sourced from Caption
            foreach (var field in this.TableSchema.Fields!.Where(f => StrFunc.IsNotEmpty(f.Caption)))
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
                   $"  @name=N'MS_Description', @value=N'{EscapeSqlString(description)}',\r\n" +
                   $"  @level0type=N'SCHEMA', @level0name=N'dbo',\r\n" +
                   $"  @level1type=N'TABLE', @level1name=N'{EscapeSqlString(tableName)}';";
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
                   $"  @name=N'MS_Description', @value=N'{EscapeSqlString(description)}',\r\n" +
                   $"  @level0type=N'SCHEMA', @level0name=N'dbo',\r\n" +
                   $"  @level1type=N'TABLE', @level1name=N'{EscapeSqlString(tableName)}',\r\n" +
                   $"  @level2type=N'COLUMN', @level2name=N'{EscapeSqlString(columnName)}';";
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
                string text = GetFieldCommandText(field);
                if (StrFunc.IsNotEmpty(text))
                {
                    if (sb.Length > 0)
                        sb.Append(",\r\n");
                    sb.Append("  " + text);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the SQL fragment for a single column definition.
        /// </summary>
        /// <param name="field">The field definition.</param>
        private static string GetFieldCommandText(DbField field)
        {
            // Column type
            string dbType = ConverDbType(field);
            // Nullability
            string allowNull = field.AllowNull ? "NULL" : "NOT NULL";
            // Default value
            string defaultValue = GetDefaultValue(field);
            string defaultText;
            if (StrFunc.IsNotEmpty(defaultValue))
                defaultText = $"DEFAULT ({defaultValue})";
            else
                defaultText = string.Empty;

            if (StrFunc.IsEmpty(defaultText))
                return $"{QuoteName(field.FieldName)} {dbType} {allowNull}";
            else
                return $"{QuoteName(field.FieldName)} {dbType} {allowNull} {defaultText}";
        }

        /// <summary>
        /// Converts a field definition to the corresponding SQL Server column type string.
        /// </summary>
        /// <param name="field">The field definition.</param>
        private static string ConverDbType(DbField field)
        {
            switch (field.DbType)
            {
                case FieldDbType.String:
                    return $"[nvarchar]({field.Length})";
                case FieldDbType.Text:
                    return "[nvarchar](max)";
                case FieldDbType.Boolean:
                    return "[bit]";
                case FieldDbType.AutoIncrement:
                    return "[int] IDENTITY(1,1)";
                case FieldDbType.Short:
                    return "[smallint]";
                case FieldDbType.Integer:
                    return "[int]";
                case FieldDbType.Long:
                    return "[bigint]";
                case FieldDbType.Decimal:
                    {
                        int precision = field.Precision > 0 ? field.Precision : 18;
                        int scale = field.Scale > 0 ? field.Scale : 0;
                        return $"[decimal]({precision},{scale})";
                    }
                case FieldDbType.Currency:
                    return "[decimal](19,4)";
                case FieldDbType.Date:
                    return "[date]";
                case FieldDbType.DateTime:
                    return "[datetime]";
                case FieldDbType.Guid:
                    return "[uniqueidentifier]";
                case FieldDbType.Binary:
                    return "[varbinary](max)";
                default:
                    throw new InvalidOperationException($"DbType={field.DbType} is not supported");
            }
        }

        /// <summary>
        /// Gets the default value expression for a field.
        /// </summary>
        /// <param name="dbField">The field definition.</param>
        private static string GetDefaultValue(DbField dbField)
        {
            if (dbField.AllowNull)
                return string.Empty;
            else
                return GetDefaultValue(dbField.DbType, dbField.DefaultValue);
        }

        /// <summary>
        /// Gets the default value expression for a given data type and raw default value.
        /// </summary>
        /// <param name="dbType">The field data type.</param>
        /// <param name="defaultValue">The raw default value.</param>
        private static string GetDefaultValue(FieldDbType dbType, string defaultValue)
        {
            string originalDefaultValue = SqlSchemaHelper.GetDefaultValueExpression(dbType);

            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return StrFunc.Format("N'{0}'", StrFunc.IsEmpty(defaultValue) ? originalDefaultValue : defaultValue);
                case FieldDbType.AutoIncrement:
                    return string.Empty;
                default:
                    return StrFunc.IsEmpty(defaultValue) ? originalDefaultValue : defaultValue;
            }
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
                fieldBuilder.Append(CultureInfo.InvariantCulture, $"{QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }

            string name = StrFunc.Format(index.Name, tableName);
            return $"CONSTRAINT {QuoteName(name)} PRIMARY KEY ({fieldBuilder})";
        }

        /// <summary>
        /// Gets the SQL statements for creating all non-primary-key indexes.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private string GetIndexsCommandText(string tableName)
        {
            var sb = new StringBuilder();
            foreach (TableSchemaIndex index in this.TableSchema.Indexes!)
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
        private static string GetIndexCommandText(string tableName, TableSchemaIndex index)
        {
            // Index name
            string name = StrFunc.Format(index.Name, tableName);
            // Index fields
            var fieldBuilder = new StringBuilder();
            foreach (IndexField field in index.IndexFields!)
            {
                if (fieldBuilder.Length > 0)
                    fieldBuilder.Append(", ");
                fieldBuilder.Append(CultureInfo.InvariantCulture, $"{QuoteName(field.FieldName)} {field.SortDirection.ToString().ToUpperInvariant()}");
            }
            // Generate the CREATE INDEX statement
            if (index.Unique)
                return $"CREATE UNIQUE INDEX {QuoteName(name)} ON {QuoteName(tableName)} ({fieldBuilder});";
            else
                return $"CREATE INDEX {QuoteName(name)} ON {QuoteName(tableName)} ({fieldBuilder});";
        }
    }
}
