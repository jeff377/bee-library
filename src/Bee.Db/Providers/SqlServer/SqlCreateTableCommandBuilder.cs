using Bee.Definition.Database;
using System;
using System.Text;
using Bee.Core;
using Bee.Core.Data;
using Bee.Definition;

using Bee.Db;
using Bee.Db.Providers;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Generates CREATE TABLE SQL statements for SQL Server.
    /// </summary>
    public class SqlCreateTableCommandBuilder : ICreateTableCommandBuilder
    {
        private TableSchema _dbTable = null;

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
            get { return _dbTable; }
        }

        /// <summary>
        /// Gets the table name.
        /// </summary>
        private string TableName
        {
            get { return this.TableSchema.TableName; }
        }

        /// <summary>
        /// Gets the SQL statement for creating or upgrading a table.
        /// </summary>
        /// <param name="dbTable">The table schema definition.</param>
        public string GetCommandText(TableSchema dbTable)
        {
            _dbTable = dbTable;

            if (this.TableSchema.UpgradeAction == DbUpgradeAction.Upgrade)
                return $"-- Upgrade table {this.TableName}\r\n{this.GetUpgradeCommandText()}";
            else
                return $"-- Create table {this.TableName}\r\n{this.GetCreateTableCommandText()}";
        }

        /// <summary>
        /// Gets the SQL script for upgrading an existing table.
        /// </summary>
        private string GetUpgradeCommandText()
        {
            var sb = new StringBuilder();
            string tmpTableName = $"tmp_{this.TableName}";
            // Drop the temporary table
            string sql = GetDropTableCommandText(tmpTableName);
            sb.AppendLine("-- Drop temporary table");
            sb.AppendLine(sql);
            // Create the temporary table
            sql = GetCreateTableCommandText(tmpTableName);
            sb.AppendLine("-- Create temporary table");
            sb.AppendLine(sql);
            // Move data
            sql = GetInsertTableCommandText(this.TableName, tmpTableName);
            sb.AppendLine("-- Move data");
            sb.AppendLine(sql);
            // Drop the old table
            sql = GetDropTableCommandText(this.TableName);
            sb.AppendLine("-- Drop old table");
            sb.AppendLine(sql);
            // Rename the temporary table
            sb.AppendLine("-- Rename temporary table");
            sql = GetRenameTableCommandText(tmpTableName, this.TableName);
            sb.AppendLine(sql);

            return sb.ToString();
        }

        /// <summary>
        /// Gets the SQL command text for dropping a table if it exists.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private string GetDropTableCommandText(string tableName)
        {
            return $"IF (SELECT COUNT(*) From sys.tables WHERE name=N'{tableName}')>0\n" +
                        $"  EXEC('DROP TABLE {tableName}');";
        }

        /// <summary>
        /// Gets the INSERT INTO ... SELECT SQL statement for migrating data from the old table to the new table.
        /// </summary>
        /// <param name="tableName">The source (old) table name.</param>
        /// <param name="newTableName">The destination (new) table name.</param>
        private string GetInsertTableCommandText(string tableName, string newTableName)
        {
           // Build the list of fields to migrate
            string fields = string.Empty;
            foreach (DbField field in this.TableSchema.Fields)
            {
                if (field.UpgradeAction != DbUpgradeAction.New && field.DbType != FieldDbType.AutoIncrement)
                {
                    if (StrFunc.IsNotEmpty(fields))
                        fields += ", ";
                    fields += $"[{field.FieldName}]";
                }
            }
            // Build the INSERT INTO ... SELECT statement
            string  sql = $"INSERT INTO [{newTableName}] ({fields}) \n" +
                                  $"SELECT {fields} FROM [{tableName}];";
            return sql;
        }

        /// <summary>
        /// Gets the SQL command text for renaming a table.
        /// </summary>
        /// <param name="tableName">The current (old) table name.</param>
        /// <param name="newTableName">The target (new) table name.</param>
        private string GetRenameTableCommandText(string tableName, string newTableName)
        {
            var sb = new StringBuilder();
            // Rename indexes
            foreach (TableSchemaIndex index in this.TableSchema.Indexes)
            {
                string oldName = StrFunc.Format(index.Name, tableName);  // Old index name
                string newName = StrFunc.Format(index.Name, newTableName);  // New index name
                sb.Append($"EXEC sp_rename N'dbo.{tableName}.{oldName}', N'{newName}', N'INDEX';\n");
            }
            // Rename the table
            sb.Append($"EXEC sp_rename N'{tableName}', N'{newTableName}';\n");
            return sb.ToString();
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
            sb.Append($"CREATE TABLE [{dbTableName}] (\r\n{fields}");
            if (StrFunc.IsNotEmpty(primaryKey))
                sb.Append($",\r\n  {primaryKey}");
            sb.Append("\r\n);");
            // Append the index creation statements
            if (StrFunc.IsNotEmpty(indexs))
                sb.Append($"\r\n{indexs}");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the SQL fragment for all column definitions.
        /// </summary>
        private string GetFieldsCommandText()
        {
            // Build the column definitions
            var sb = new StringBuilder();
            foreach (DbField field in this.TableSchema.Fields)
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
        private string GetFieldCommandText(DbField field)
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
                return $"[{field.FieldName}] {dbType} {allowNull}";
            else
                return $"[{field.FieldName}] {dbType} {allowNull} {defaultText}";
        }

        /// <summary>
        /// Converts a field definition to the corresponding SQL Server column type string.
        /// </summary>
        /// <param name="field">The field definition.</param>
        private string ConverDbType(DbField field)
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
        private string GetDefaultValue(DbField dbField)
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
        private string GetDefaultValue(FieldDbType dbType, string defaultValue)
        {
            string originalDefaultValue = DbFunc.GetSqlDefaultValue(dbType);

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
            string fields = string.Empty;
            foreach (IndexField field in index.IndexFields)
            {
                if (StrFunc.IsNotEmpty(fields))
                    fields += ", ";
                fields += $"[{field.FieldName}] {field.SortDirection.ToString().ToUpper()}";
            }

            string name = StrFunc.Format(index.Name, tableName);
            return $"CONSTRAINT [{name}] PRIMARY KEY ({fields})";
        }

        /// <summary>
        /// Gets the SQL statements for creating all non-primary-key indexes.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private string GetIndexsCommandText(string tableName)
        {
            var sb = new StringBuilder();
            foreach (TableSchemaIndex index in this.TableSchema.Indexes)
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
        private string GetIndexCommandText(string tableName, TableSchemaIndex index)
        {
            // Index name
            string name = StrFunc.Format(index.Name, tableName);
            // Index fields
            string fields = string.Empty;
            foreach (IndexField field in index.IndexFields)
            {
                if (StrFunc.IsNotEmpty(fields))
                    fields += ", ";
                fields += $"[{field.FieldName}] {field.SortDirection.ToString().ToUpper()}";
            }
            // Generate the CREATE INDEX statement
            if (index.Unique)
                return $"CREATE UNIQUE INDEX [{name}] ON [{tableName}] ({fields});";
            else
                return $"CREATE INDEX [{name}] ON [{tableName}] ({fields});";
        }
    }
}
