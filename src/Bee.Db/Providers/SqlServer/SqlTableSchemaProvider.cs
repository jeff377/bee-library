using Bee.Definition.Database;
using Bee.Base;
using Bee.Base.Data;
using Bee.Definition;
using System.Data;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Provides methods for reading and parsing SQL Server table schemas.
    /// </summary>
    public class SqlTableSchemaProvider
    {
        private readonly DbAccess _dbAccess;

        #region 建構函式

        /// <summary>
        /// Initializes a new instance of <see cref="SqlTableSchemaProvider"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public SqlTableSchemaProvider(string databaseId)
        {
            DatabaseId = databaseId;
            _dbAccess = new DbAccess(databaseId);
        }

        #endregion

        /// <summary>
        /// Gets the database identifier.
        /// </summary>
        public string DatabaseId { get; }

        /// <summary>
        /// Gets the schema definition for the specified table.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        public TableSchema GetTableSchema(string tableName)
        {
            // Return null if the table does not exist
            if (!TableExists(tableName)) { return null; }

            var dbTable = new TableSchema();
            dbTable.TableName = tableName;

            // Retrieve the index data source
            var indexes = GetTableIndexes(tableName);
            // Parse the primary key
            ParsePrimaryKey(dbTable, indexes);
            // Parse the remaining indexes
            ParseIndexes(dbTable, indexes);

            // Retrieve the column data source
            var columns = GetColumns(tableName);
            foreach (DataRow row in columns.Rows)
            {
                var dbField = ParseDbField(row);
                dbTable.Fields.Add(dbField);
            }

            return dbTable;
        }

        /// <summary>
        /// Determines whether the specified table exists in the database.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private bool TableExists(string tableName)
        {
            string sql = "Select Count(*) From sys.tables A Where A.name={0}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, tableName);
            var result = _dbAccess.Execute(command);
            int count = BaseFunc.CInt(result.Scalar);
            return count > 0;
        }

        /// <summary>
        /// Gets the index information for the specified table.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private DataTable GetTableIndexes(string tableName)
        {
            string sql = "SELECT A.name as FieldName,D.is_primary_key as IsPrimaryKey,D.is_unique as IsUnique,D.name,\n" +
                          "C.key_ordinal as KeyOrdinal,C.is_descending_key As IsDesc \n" +
                          "FROM sys.columns A  \n" +
                          "INNER JOIN sys.tables B On A.object_id=B.object_id \n" +
                          "INNER JOIN sys.index_columns C On A.object_id=C.object_id And A.column_id=C.column_id \n" +
                          "LEFT JOIN sys.indexes D On A.object_id=D.object_id And C.index_id=D.index_id \n" +
                          "WHERE B.name={0} \n" +
                          "Order By D.is_primary_key,C.key_ordinal";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, tableName);
            var result = _dbAccess.Execute(command);
            var table = result.Table;
            table.TableName = "TableIndex";
            return table;
        }

        /// <summary>
        /// Parses and populates the primary key from the index data.
        /// </summary>
        /// <param name="dbTable">The table schema to populate.</param>
        /// <param name="table">The index data table.</param>
        private void ParsePrimaryKey(TableSchema dbTable, DataTable table)
        {
            if (table.IsEmpty()) { return; }

            table.DefaultView.RowFilter = "IsPrimaryKey=true";
            table.DefaultView.Sort = "KeyOrdinal";
            if (table.DefaultView.IsEmpty()) { return; }

            // Get the index name
            string name = BaseFunc.CStr(table.DefaultView[0]["name"]);
            // 取得主索引
            var tableIndex = new TableSchemaIndex();
            tableIndex.PrimaryKey = true;
            tableIndex.Name = name;
            tableIndex.Unique = true;
            dbTable.Indexes.Add(tableIndex);
            foreach (DataRowView row in table.DefaultView)
            {
                var indexField = new IndexField();
                indexField.FieldName = BaseFunc.CStr(row["FieldName"]);
                indexField.SortDirection = BaseFunc.CBool(row["IsDesc"]) ? SortDirection.Desc : SortDirection.Asc;
                tableIndex.IndexFields.Add(indexField);
            }
            // Remove the processed primary key rows
            table.DefaultView.DeleteRows(true);
        }

        /// <summary>
        /// Parses and populates all remaining indexes from the index data.
        /// </summary>
        /// <param name="dbTable">The table schema to populate.</param>
        /// <param name="table">The index data table.</param>
        private void ParseIndexes(TableSchema dbTable, DataTable table)
        {
            while (!table.IsEmpty())
            {
                var oRow = table.Rows[0];
                string name = BaseFunc.CStr(oRow["Name"]);  // Get the index name
                bool isUnique = BaseFunc.CBool(oRow["IsUnique"]);

                var tableIndex = new TableSchemaIndex();
                tableIndex.Name = name;
                tableIndex.Unique = isUnique;
                dbTable.Indexes.Add(tableIndex);

                table.DefaultView.RowFilter = $"Name='{name.Replace("'", "''")}'";

                table.DefaultView.Sort = "Name,KeyOrdinal";
                foreach (DataRowView rowView in table.DefaultView)
                {
                    var indexField = new IndexField();
                    indexField.FieldName = BaseFunc.CStr(rowView["FieldName"]);
                    indexField.SortDirection = BaseFunc.CBool(rowView["IsDesc"]) ? SortDirection.Desc : SortDirection.Asc;
                    tableIndex.IndexFields.Add(indexField);
                }
                // Remove the processed index rows
                table.DefaultView.DeleteRows(true);
            }
        }

        /// <summary>
        /// Gets the column list for the specified table.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private DataTable GetColumns(string tableName)
        {
            string sql = "SELECT A.is_nullable as AllowDBNull,A.is_identity as AutoIncrement, \n" +
                          "IsNull(C.seed_value,0) as AutoIncrementSeed,IsNull(C.increment_value,0) as AutoIncrementStep, \n" +
                          "IsNull(E.name,'') as BindDefault,TYPE_NAME(A.system_type_id) as DbType,A.precision,A.scale as Decimals, \n" +
                          "IsNull(F.definition,'') as DefaultValue,A.name as FieldName,A.max_length as Length, \n" +
                          "IsNull((select value from fn_listextendedproperty(NULL, 'user', 'dbo', 'table', B.name, 'column', A.name)),'') as Description \n" +
                          "FROM sys.columns A \n" +
                          "INNER JOIN sys.tables B On A.object_id=B.object_id \n" +
                          "LEFT JOIN sys.identity_columns C on A.object_id=C.object_id and A.column_id=C.column_id \n" +
                          "LEFT JOIN sys.objects E on E.object_id = A.default_object_id and E.type = 'D' \n" +
                          "LEFT JOIN sys.default_constraints F on F.parent_object_id = A.object_id and F.parent_column_id = A.column_id \n" +
                          "WHERE B.name={0} \n" +
                          "ORDER BY A.column_id";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, tableName);
            var result = _dbAccess.Execute(command);
            var table = result.Table;
            table.TableName = "Columns";
            return table;
        }

        /// <summary>
        /// Creates a field definition from the column data row.
        /// </summary>
        /// <param name="row">The data row containing column information.</param>
        private DbField ParseDbField(DataRow row)
        {
            var dbField = new DbField();
            dbField.FieldName = row.GetFieldValue<string>("FieldName");
            dbField.Caption = row.GetFieldValue<string>("Description");
            dbField.AllowNull = row.GetFieldValue<bool>("AllowDBNull");

            if (row.GetFieldValue<bool>("AutoIncrement"))
                dbField.DbType = FieldDbType.AutoIncrement;
            else
                dbField.DbType = GetFieldDbType(
                    row.GetFieldValue<string>("DbType"),
                    row.GetFieldValue<int>("precision"),
                    row.GetFieldValue<int>("Decimals"),
                    row.GetFieldValue<int>("Length"));

            // Set the String field length
            if (dbField.DbType == FieldDbType.String)
            {
                if (StrFunc.ToUpper(row.GetFieldValue<string>("DbType")) == "NVARCHAR")
                    dbField.Length = row.GetFieldValue<int>("Length") / 2;
                else
                    dbField.Length = row.GetFieldValue<int>("Length");
            }

            // Set the Decimal precision and scale
            if (dbField.DbType == FieldDbType.Decimal)
            {
                dbField.Precision = row.GetFieldValue<int>("Precision");
                dbField.Scale = row.GetFieldValue<int>("Decimals");
            }

            string originalDefaultValue = DbFunc.GetSqlDefaultValue(dbField.DbType);  // Get the built-in default value
            dbField.DefaultValue = ParseDBDefaultValue(row.GetFieldValue<string>("DbType"), row.GetFieldValue<string>("DefaultValue"), originalDefaultValue);
            return dbField;
        }

        /// <summary>
        /// Converts a SQL Server data type string to the corresponding <see cref="FieldDbType"/> enum value.
        /// </summary>
        /// <param name="dataType">The SQL Server data type name.</param>
        /// <param name="dataPrecision">The numeric precision.</param>
        /// <param name="dataScale">The number of decimal places.</param>
        /// <param name="length">The data length.</param>
        public static FieldDbType GetFieldDbType(string dataType, int dataPrecision, int dataScale, int length)
        {
            switch (StrFunc.ToUpper(dataType))
            {
                case "NCHAR":
                    return FieldDbType.String;
                case "NVARCHAR":
                    if (length == -1)
                        return FieldDbType.Text;
                    else
                        return FieldDbType.String;
                case "BIT":
                    return FieldDbType.Boolean;
                case "SMALLINT":
                    return FieldDbType.Short;
                case "INT":
                    return FieldDbType.Integer;
                case "BIGINT":
                    return FieldDbType.Long;
                case "FLOAT":
                    return FieldDbType.Decimal;
                case "DECIMAL":
                    if (dataPrecision == 19 && dataScale == 4)
                        return FieldDbType.Currency;
                    else
                        return FieldDbType.Decimal;
                case "DATE":
                    return FieldDbType.Date;
                case "DATETIME":
                    return FieldDbType.DateTime;
                case "UNIQUEIDENTIFIER":
                    return FieldDbType.Guid;
                case "VARBINARY":
                    return FieldDbType.Binary;
                default:
                    return FieldDbType.Unknown;
            }
        }

        /// <summary>
        /// Parses the field default value retrieved from the database.
        /// Returns an empty string if the value matches the built-in default.
        /// </summary>
        /// <param name="dataType">The SQL Server data type name.</param>
        /// <param name="defaultValue">The actual default value from the database.</param>
        /// <param name="originalDefaultValue">The built-in default value.</param>
        public string ParseDBDefaultValue(string dataType, string defaultValue, string originalDefaultValue)
        {
            switch (StrFunc.ToUpper(dataType))
            {
                case "CHAR":
                case "VARCHAR":
                    defaultValue = StrFunc.LeftRightCut(defaultValue, "('", "')");
                    break;
                case "NCHAR":
                case "NVARCHAR":
                    defaultValue = StrFunc.LeftRightCut(defaultValue, "(N'", "')");
                    defaultValue = StrFunc.LeftRightCut(defaultValue, "('", "')");
                    break;
                case "BIT":
                case "INT":
                case "MONEY":
                case "FLOAT":
                    defaultValue = StrFunc.LeftRightCut(defaultValue, "((", "))");
                    break;
                case "DATE":
                case "DATETIME":
                case "UNIQUEIDENTIFIER":
                    defaultValue = StrFunc.LeftRightCut(defaultValue, "(", ")");
                    break;
                default:
                    defaultValue = string.Empty;
                    break;
            }

            // Return empty if the database default matches the built-in default.
            if (StrFunc.Equals(originalDefaultValue, defaultValue))
                return string.Empty;
            else
                return defaultValue;
        }

    }
}
