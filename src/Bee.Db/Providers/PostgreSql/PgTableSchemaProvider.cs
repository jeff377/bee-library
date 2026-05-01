using System.Data;
using Bee.Base;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Definition.Database;
using Bee.Definition.Sorting;

namespace Bee.Db.Providers.PostgreSql
{
    /// <summary>
    /// Provides methods for reading and parsing PostgreSQL table schemas.
    /// Counterpart to <see cref="SqlServer.SqlTableSchemaProvider"/>; queries
    /// <c>information_schema</c> for cross-DB-standard metadata and
    /// <c>pg_catalog</c> for PG-specific features (descriptions, index columns).
    /// Operates against the <c>public</c> schema.
    /// </summary>
    public class PgTableSchemaProvider : ITableSchemaProvider
    {
        private const string DefaultSchema = "public";
        private readonly DbAccess _dbAccess;

        /// <summary>
        /// Initializes a new instance of <see cref="PgTableSchemaProvider"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public PgTableSchemaProvider(string databaseId)
        {
            DatabaseId = databaseId;
            _dbAccess = new DbAccess(databaseId);
        }

        /// <summary>
        /// Gets the database identifier.
        /// </summary>
        public string DatabaseId { get; }

        /// <summary>
        /// Gets the schema definition for the specified table, or null if the table does not exist.
        /// </summary>
        /// <param name="tableName">The table name.</param>
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
        /// Determines whether the specified table exists in the public schema.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private bool TableExists(string tableName)
        {
            string sql = "SELECT COUNT(*) FROM information_schema.tables " +
                         "WHERE table_schema={0} AND table_name={1}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, DefaultSchema, tableName);
            var result = _dbAccess.Execute(command);
            return BaseFunc.CInt(result.Scalar!) > 0;
        }

        /// <summary>
        /// Gets the table-level comment (PG <c>COMMENT ON TABLE</c>) value.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private string GetTableDescription(string tableName)
        {
            string sql = "SELECT COALESCE(obj_description(({0}||'.'||{1})::regclass, 'pg_class'), '')";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, DefaultSchema, tableName);
            var result = _dbAccess.Execute(command);
            return BaseFunc.CStr(result.Scalar!);
        }

        /// <summary>
        /// Gets the index information for the specified table, including the primary key.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private DataTable GetTableIndexes(string tableName)
        {
            // pg_index.indkey is a 0-indexed int2vector of column ordinals; we expand it via
            // generate_subscripts so each row represents one (index, column) pair with its ordinal
            // position. indoption is parallel to indkey and shares the same 0-indexed access; bit 0
            // of each entry is the DESC flag for that column.
            string sql =
                "SELECT a.attname AS \"FieldName\", " +
                "       i.indisprimary AS \"IsPrimaryKey\", " +
                "       i.indisunique AS \"IsUnique\", " +
                "       c.relname AS \"name\", " +
                "       k.ord AS \"KeyOrdinal\", " +
                "       (COALESCE(i.indoption[k.ord], 0) & 1) = 1 AS \"IsDesc\" " +
                "FROM pg_catalog.pg_index i " +
                "JOIN pg_catalog.pg_class c ON c.oid = i.indexrelid " +
                "JOIN pg_catalog.pg_class t ON t.oid = i.indrelid " +
                "JOIN pg_catalog.pg_namespace n ON n.oid = t.relnamespace " +
                "JOIN LATERAL generate_subscripts(i.indkey, 1) AS k(ord) ON TRUE " +
                "JOIN pg_catalog.pg_attribute a ON a.attrelid = t.oid AND a.attnum = i.indkey[k.ord] " +
                "WHERE n.nspname = {0} AND t.relname = {1} " +
                "ORDER BY i.indisprimary DESC, c.relname, k.ord";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, DefaultSchema, tableName);
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

            string name = BaseFunc.CStr(table.DefaultView[0]["name"]);
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
        /// Parses and populates all remaining indexes from the index data.
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
        /// identity flag, default expression, and column comment.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private DataTable GetColumns(string tableName)
        {
            string sql =
                "SELECT c.column_name AS \"FieldName\", " +
                "       c.data_type AS \"DbType\", " +
                "       (c.is_nullable = 'YES') AS \"AllowDBNull\", " +
                "       (c.is_identity = 'YES') AS \"AutoIncrement\", " +
                "       COALESCE(c.character_maximum_length, 0) AS \"Length\", " +
                "       COALESCE(c.numeric_precision, 0) AS \"Precision\", " +
                "       COALESCE(c.numeric_scale, 0) AS \"Decimals\", " +
                "       COALESCE(c.column_default, '') AS \"DefaultValue\", " +
                "       COALESCE(col_description((c.table_schema||'.'||c.table_name)::regclass, c.ordinal_position), '') AS \"Description\" " +
                "FROM information_schema.columns c " +
                "WHERE c.table_schema = {0} AND c.table_name = {1} " +
                "ORDER BY c.ordinal_position";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, DefaultSchema, tableName);
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

            string originalDefaultValue = PgSchemaSyntax.GetDefaultValueExpression(dbField.DbType);
            dbField.DefaultValue = ParseDBDefaultValue(dataType, row.GetFieldValue<string>("DefaultValue"), originalDefaultValue);
            return dbField;
        }

        /// <summary>
        /// Converts a PostgreSQL <c>information_schema.columns.data_type</c> value to the
        /// corresponding <see cref="FieldDbType"/> enum value.
        /// </summary>
        /// <param name="dataType">The PostgreSQL data type name (e.g. <c>character varying</c>).</param>
        /// <param name="dataPrecision">The numeric precision.</param>
        /// <param name="dataScale">The number of decimal places.</param>
        /// <param name="length">The character maximum length (0 when not applicable).</param>
        public static FieldDbType GetFieldDbType(string dataType, int dataPrecision, int dataScale, int length)
        {
            switch ((dataType ?? string.Empty).ToLower())
            {
                case "character":
                case "char":
                case "character varying":
                case "varchar":
                    return length > 0 ? FieldDbType.String : FieldDbType.Text;
                case "text":
                    return FieldDbType.Text;
                case "boolean":
                case "bool":
                    return FieldDbType.Boolean;
                case "smallint":
                case "int2":
                    return FieldDbType.Short;
                case "integer":
                case "int":
                case "int4":
                    return FieldDbType.Integer;
                case "bigint":
                case "int8":
                    return FieldDbType.Long;
                case "numeric":
                case "decimal":
                    if (dataPrecision == 19 && dataScale == 4)
                        return FieldDbType.Currency;
                    return FieldDbType.Decimal;
                case "real":
                case "double precision":
                case "float4":
                case "float8":
                    return FieldDbType.Decimal;
                case "date":
                    return FieldDbType.Date;
                case "timestamp":
                case "timestamp without time zone":
                case "timestamp with time zone":
                    return FieldDbType.DateTime;
                case "uuid":
                    return FieldDbType.Guid;
                case "bytea":
                    return FieldDbType.Binary;
                default:
                    return FieldDbType.Unknown;
            }
        }

        /// <summary>
        /// Parses the field default value retrieved from <c>information_schema.columns.column_default</c>,
        /// stripping PostgreSQL-specific cast suffixes (e.g. <c>'abc'::character varying</c> → <c>abc</c>).
        /// Returns an empty string if the value matches the framework built-in default.
        /// </summary>
        /// <param name="dataType">The PostgreSQL data type name.</param>
        /// <param name="defaultValue">The actual default value from the database.</param>
        /// <param name="originalDefaultValue">The framework built-in default value.</param>
        public static string ParseDBDefaultValue(string dataType, string defaultValue, string originalDefaultValue)
        {
            if (StringUtilities.IsEmpty(defaultValue)) return string.Empty;

            // Strip an optional ::type cast suffix (e.g. 'abc'::character varying, '0'::integer).
            int castIndex = defaultValue.IndexOf("::", StringComparison.Ordinal);
            string trimmed = castIndex >= 0 ? defaultValue.Substring(0, castIndex) : defaultValue;
            trimmed = trimmed.Trim();

            // For string-like types: strip the surrounding single quotes and unescape doubled quotes.
            string normalized;
            switch ((dataType ?? string.Empty).ToLower())
            {
                case "character":
                case "char":
                case "character varying":
                case "varchar":
                case "text":
                    normalized = StripStringLiteral(trimmed);
                    break;
                default:
                    normalized = trimmed;
                    break;
            }

            return StringUtilities.IsEquals(originalDefaultValue, normalized) ? string.Empty : normalized;
        }

        /// <summary>
        /// Strips the surrounding single quotes and unescapes doubled quotes for a PG string literal.
        /// </summary>
        private static string StripStringLiteral(string value)
        {
            if (value.Length >= 2 && value.StartsWith('\'') && value.EndsWith('\''))
            {
                string inner = value.Substring(1, value.Length - 2);
                return inner.Replace("''", "'");
            }
            return value;
        }
    }
}
