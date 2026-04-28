using System.Data;
using Bee.Base;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Definition.Database;
using Bee.Definition.Sorting;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// MySQL 8.0+ schema provider. Reads <c>INFORMATION_SCHEMA</c>
    /// (<c>COLUMNS</c>, <c>STATISTICS</c>, <c>TABLES</c>) bound to the connection's current
    /// database (<c>DATABASE()</c>). Counterpart to
    /// <see cref="PostgreSql.PgTableSchemaProvider"/>; PG is the closer reference because
    /// both query <c>INFORMATION_SCHEMA</c>.
    /// </summary>
    /// <remarks>
    /// MySQL has no schema-in-database concept (TABLE_SCHEMA == database name), so this
    /// provider does not take a schema parameter and always scopes queries to
    /// <c>DATABASE()</c>.
    /// </remarks>
    public class MySqlTableSchemaProvider : ITableSchemaProvider
    {
        private readonly DbAccess _dbAccess;

        /// <summary>
        /// Initializes a new instance of <see cref="MySqlTableSchemaProvider"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public MySqlTableSchemaProvider(string databaseId)
        {
            DatabaseId = databaseId;
            _dbAccess = new DbAccess(databaseId);
        }

        /// <inheritdoc />
        public string DatabaseId { get; }

        /// <inheritdoc />
        public TableSchema? GetTableSchema(string tableName)
        {
            if (!TableExists(tableName)) return null;

            var dbTable = new TableSchema { TableName = tableName };
            dbTable.DisplayName = GetTableDescription(tableName);

            var indexes = GetTableIndexes(tableName);
            ParsePrimaryKey(dbTable, indexes);
            ParseIndexes(dbTable, indexes);

            var columns = GetColumns(tableName);
            foreach (DataRow row in columns.Rows)
            {
                dbTable.Fields!.Add(ParseDbField(row));
            }

            return dbTable;
        }

        /// <summary>
        /// Determines whether the specified table exists in the connection's current database.
        /// </summary>
        private bool TableExists(string tableName)
        {
            string sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES " +
                         "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = {0}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, tableName);
            var result = _dbAccess.Execute(command);
            return BaseFunc.CInt(result.Scalar!) > 0;
        }

        /// <summary>
        /// Gets the table-level <c>COMMENT</c> value (empty string if none).
        /// </summary>
        private string GetTableDescription(string tableName)
        {
            string sql = "SELECT COALESCE(TABLE_COMMENT, '') FROM INFORMATION_SCHEMA.TABLES " +
                         "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = {0}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, tableName);
            var result = _dbAccess.Execute(command);
            return BaseFunc.CStr(result.Scalar!);
        }

        /// <summary>
        /// Gets the index information for the specified table, including the primary key.
        /// MySQL identifies the primary key by <c>INDEX_NAME = 'PRIMARY'</c>; <c>NON_UNIQUE = 0</c>
        /// marks unique indexes; <c>COLLATION = 'D'</c> marks DESC sort (otherwise ASC).
        /// </summary>
        private DataTable GetTableIndexes(string tableName)
        {
            string sql =
                "SELECT INDEX_NAME AS `Name`, " +
                "       (INDEX_NAME = 'PRIMARY') AS `IsPrimaryKey`, " +
                "       (NON_UNIQUE = 0) AS `IsUnique`, " +
                "       COLUMN_NAME AS `FieldName`, " +
                "       SEQ_IN_INDEX AS `KeyOrdinal`, " +
                "       (COLLATION = 'D') AS `IsDesc` " +
                "FROM INFORMATION_SCHEMA.STATISTICS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = {0} " +
                "ORDER BY (INDEX_NAME = 'PRIMARY') DESC, INDEX_NAME, SEQ_IN_INDEX";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, tableName);
            var result = _dbAccess.Execute(command);
            var table = result.Table!;
            table.TableName = "TableIndex";
            return table;
        }

        /// <summary>
        /// Parses and populates the primary key from the index data.
        /// </summary>
        private static void ParsePrimaryKey(TableSchema dbTable, DataTable table)
        {
            if (table.IsEmpty()) return;

            table.DefaultView.RowFilter = "IsPrimaryKey=true";
            table.DefaultView.Sort = "KeyOrdinal";
            if (table.DefaultView.IsEmpty()) return;

            string name = BaseFunc.CStr(table.DefaultView[0]["Name"]);
            var tableIndex = new TableSchemaIndex
            {
                PrimaryKey = true,
                Name = name,
                Unique = true
            };
            dbTable.Indexes!.Add(tableIndex);
            foreach (DataRowView row in table.DefaultView)
            {
                var indexField = new IndexField
                {
                    FieldName = BaseFunc.CStr(row["FieldName"]),
                    SortDirection = BaseFunc.CBool(row["IsDesc"]) ? SortDirection.Desc : SortDirection.Asc
                };
                tableIndex.IndexFields!.Add(indexField);
            }
            table.DefaultView.DeleteRows(true);
        }

        /// <summary>
        /// Parses and populates all remaining (non-PK) indexes from the index data.
        /// </summary>
        private static void ParseIndexes(TableSchema dbTable, DataTable table)
        {
            while (!table.IsEmpty())
            {
                var oRow = table.Rows[0];
                string name = BaseFunc.CStr(oRow["Name"]);
                bool isUnique = BaseFunc.CBool(oRow["IsUnique"]);

                var tableIndex = new TableSchemaIndex
                {
                    Name = name,
                    Unique = isUnique
                };
                dbTable.Indexes!.Add(tableIndex);

                table.DefaultView.RowFilter = $"Name='{name.Replace("'", "''")}'";
                table.DefaultView.Sort = "Name,KeyOrdinal";
                foreach (DataRowView rowView in table.DefaultView)
                {
                    var indexField = new IndexField
                    {
                        FieldName = BaseFunc.CStr(rowView["FieldName"]),
                        SortDirection = BaseFunc.CBool(rowView["IsDesc"]) ? SortDirection.Desc : SortDirection.Asc
                    };
                    tableIndex.IndexFields!.Add(indexField);
                }
                table.DefaultView.DeleteRows(true);
            }
        }

        /// <summary>
        /// Gets the column list for the specified table, including type, nullability,
        /// AUTO_INCREMENT flag (extracted from <c>EXTRA</c>), default expression, and
        /// column comment.
        /// </summary>
        private DataTable GetColumns(string tableName)
        {
            // CHARACTER_MAXIMUM_LENGTH on LONGTEXT/LONGBLOB is 4294967295 (UINT32 max),
            // which overflows Int32. Force 0 for blob/text families so the framework's
            // Int32 mapping is safe; their length isn't meaningful in TableSchema anyway.
            string sql =
                "SELECT c.COLUMN_NAME AS `FieldName`, " +
                "       c.DATA_TYPE AS `DbType`, " +
                "       (c.IS_NULLABLE = 'YES') AS `AllowDBNull`, " +
                "       (LOCATE('auto_increment', LOWER(c.EXTRA)) > 0) AS `AutoIncrement`, " +
                "       CASE WHEN c.DATA_TYPE IN ('text','tinytext','mediumtext','longtext'," +
                "                                  'blob','tinyblob','mediumblob','longblob') THEN 0 " +
                "            ELSE COALESCE(c.CHARACTER_MAXIMUM_LENGTH, 0) " +
                "       END AS `Length`, " +
                "       COALESCE(c.NUMERIC_PRECISION, 0) AS `Precision`, " +
                "       COALESCE(c.NUMERIC_SCALE, 0) AS `Decimals`, " +
                "       COALESCE(c.COLUMN_DEFAULT, '') AS `DefaultValue`, " +
                "       COALESCE(c.COLUMN_COMMENT, '') AS `Description` " +
                "FROM INFORMATION_SCHEMA.COLUMNS c " +
                "WHERE c.TABLE_SCHEMA = DATABASE() AND c.TABLE_NAME = {0} " +
                "ORDER BY c.ORDINAL_POSITION";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, tableName);
            var result = _dbAccess.Execute(command);
            var table = result.Table!;
            table.TableName = "Columns";
            return table;
        }

        /// <summary>
        /// Creates a field definition from the column data row.
        /// </summary>
        private static DbField ParseDbField(DataRow row)
        {
            var dbField = new DbField
            {
                FieldName = row.GetFieldValue<string>("FieldName"),
                Caption = row.GetFieldValue<string>("Description"),
                AllowNull = row.GetFieldValue<bool>("AllowDBNull")
            };

            string dataType = row.GetFieldValue<string>("DbType");
            int precision = row.GetFieldValue<int>("Precision");
            int scale = row.GetFieldValue<int>("Decimals");
            int length = row.GetFieldValue<int>("Length");

            if (row.GetFieldValue<bool>("AutoIncrement"))
                dbField.DbType = FieldDbType.AutoIncrement;
            else
                dbField.DbType = GetFieldDbType(dataType, precision, scale, length);

            if (dbField.DbType == FieldDbType.String)
                dbField.Length = length;

            if (dbField.DbType == FieldDbType.Decimal)
            {
                dbField.Precision = precision;
                dbField.Scale = scale;
            }

            string originalDefaultValue = MySqlSchemaHelper.GetDefaultValueExpression(dbField.DbType);
            dbField.DefaultValue = ParseDBDefaultValue(dataType, row.GetFieldValue<string>("DefaultValue"), originalDefaultValue);
            return dbField;
        }

        /// <summary>
        /// Converts a MySQL <c>INFORMATION_SCHEMA.COLUMNS.DATA_TYPE</c> value to the
        /// corresponding <see cref="FieldDbType"/>.
        /// </summary>
        /// <remarks>
        /// MySQL has no native <c>BOOLEAN</c>; <c>TINYINT</c> is mapped to
        /// <see cref="FieldDbType.Boolean"/> because the framework's
        /// <see cref="MySqlTypeMapping"/> only ever emits <c>TINYINT(1)</c> for boolean
        /// columns. <c>CHAR</c> with length 36 is treated as <see cref="FieldDbType.Guid"/>
        /// to round-trip the framework's default <c>CHAR(36)</c> Guid storage.
        /// </remarks>
        public static FieldDbType GetFieldDbType(string dataType, int dataPrecision, int dataScale, int length)
        {
            switch (StrFunc.ToLower(dataType))
            {
                case "char":
                    return length == 36 ? FieldDbType.Guid : FieldDbType.String;
                case "varchar":
                    return FieldDbType.String;
                case "text":
                case "tinytext":
                case "mediumtext":
                case "longtext":
                    return FieldDbType.Text;
                case "tinyint":
                    return FieldDbType.Boolean;
                case "smallint":
                    return FieldDbType.Short;
                case "int":
                case "integer":
                case "mediumint":
                    return FieldDbType.Integer;
                case "bigint":
                    return FieldDbType.Long;
                case "decimal":
                case "numeric":
                    if (dataPrecision == 19 && dataScale == 4)
                        return FieldDbType.Currency;
                    return FieldDbType.Decimal;
                case "float":
                case "double":
                case "real":
                    return FieldDbType.Decimal;
                case "date":
                    return FieldDbType.Date;
                case "datetime":
                case "timestamp":
                    return FieldDbType.DateTime;
                case "binary":
                case "varbinary":
                case "blob":
                case "tinyblob":
                case "mediumblob":
                case "longblob":
                    return FieldDbType.Binary;
                default:
                    return FieldDbType.Unknown;
            }
        }

        /// <summary>
        /// Parses the field default value retrieved from
        /// <c>INFORMATION_SCHEMA.COLUMNS.COLUMN_DEFAULT</c>. MySQL stores string-literal
        /// defaults as the value itself (no surrounding quotes) and expression defaults
        /// as the lower-case expression text (e.g. <c>uuid()</c>,
        /// <c>CURRENT_TIMESTAMP(6)</c>). Returns an empty string when the value matches
        /// the framework's built-in default.
        /// </summary>
        public static string ParseDBDefaultValue(string dataType, string defaultValue, string originalDefaultValue)
        {
            if (StrFunc.IsEmpty(defaultValue)) return string.Empty;

            string trimmed = defaultValue.Trim();

            // MySQL 8.0 INFORMATION_SCHEMA.COLUMN_DEFAULT normalises expression defaults:
            // outer parentheses are stripped (DEFAULT (UUID()) → 'uuid()') and function names
            // are returned in lower case. Normalise the framework's expected default the same
            // way (strip outer parens) and compare case-insensitively so "(UUID())" matches
            // the stored "uuid()". String literals are returned unquoted, so direct equality
            // works for them.
            string normalizedOriginal = StripOuterParens(originalDefaultValue.Trim());
            return string.Equals(normalizedOriginal, trimmed, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : trimmed;
        }

        /// <summary>
        /// Returns the value with a single layer of surrounding <c>(</c>...<c>)</c> stripped,
        /// or the value unchanged if it is not parenthesised. Used to align the framework's
        /// expression-default form (<c>(UUID())</c>) with MySQL's <c>INFORMATION_SCHEMA</c>
        /// stored form (<c>uuid()</c>).
        /// </summary>
        private static string StripOuterParens(string value)
        {
            if (value.Length >= 2 && value[0] == '(' && value[value.Length - 1] == ')')
                return value.Substring(1, value.Length - 2);
            return value;
        }
    }
}
