using System.Data;
using Bee.Base;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Definition.Database;
using Bee.Definition.Sorting;

namespace Bee.Db.Providers.Oracle
{
    /// <summary>
    /// Oracle 19c+ schema provider. Reads <c>USER_*</c> data-dictionary views and maps
    /// the result onto <see cref="TableSchema"/>. Counterpart to
    /// <see cref="MySql.MySqlTableSchemaProvider"/> and
    /// <see cref="PostgreSql.PgTableSchemaProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <c>USER_*</c> views (implicitly scoped to the connecting user's own schema)
    /// rather than <c>ALL_*</c>. The framework operates on a single-schema assumption;
    /// cross-schema scenarios are out of scope (callers must qualify
    /// <c>"OWNER"."TABLE"</c> themselves), which makes the simpler <c>USER_*</c> form
    /// equivalent and avoids per-query <c>WHERE OWNER = SYS_CONTEXT('USERENV',
    /// 'CURRENT_SCHEMA')</c> clutter.
    /// </para>
    /// <para>
    /// Identifier case: <c>USER_TAB_COLUMNS.TABLE_NAME</c> stores the unquoted (uppercased)
    /// form by default, but the framework's CREATE TABLE builder always quotes identifiers,
    /// so a <c>"st_user"</c> table is stored as the lowercase string <c>st_user</c>.
    /// Lookups pass the table name as-is; mixed-case schemas (against FormSchema convention)
    /// will not round-trip cleanly.
    /// </para>
    /// </remarks>
    public class OracleTableSchemaProvider : ITableSchemaProvider
    {
        private readonly DbAccess _dbAccess;

        /// <summary>
        /// Initializes a new instance of <see cref="OracleTableSchemaProvider"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public OracleTableSchemaProvider(string databaseId)
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
        /// Determines whether the specified table exists in the user's own schema.
        /// </summary>
        private bool TableExists(string tableName)
        {
            string sql = "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = {0}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, tableName);
            var result = _dbAccess.Execute(command);
            return BaseFunc.CInt(result.Scalar!) > 0;
        }

        /// <summary>
        /// Gets the table-level <c>COMMENT ON TABLE</c> value (empty string if none).
        /// </summary>
        private string GetTableDescription(string tableName)
        {
            string sql = "SELECT COALESCE(COMMENTS, '') FROM USER_TAB_COMMENTS WHERE TABLE_NAME = {0}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, tableName);
            var result = _dbAccess.Execute(command);
            return BaseFunc.CStr(result.Scalar!);
        }

        /// <summary>
        /// Gets the index information for the specified table, including the primary key.
        /// Joins <c>USER_INDEXES</c> + <c>USER_IND_COLUMNS</c> for the column list and
        /// <c>USER_CONSTRAINTS</c> to flag the PK index (Oracle creates the PK as a
        /// regular unique index whose name matches the constraint).
        /// </summary>
        private DataTable GetTableIndexes(string tableName)
        {
            string sql =
                "SELECT i.INDEX_NAME AS \"Name\", " +
                "       CASE WHEN c.CONSTRAINT_TYPE = 'P' THEN 1 ELSE 0 END AS \"IsPrimaryKey\", " +
                "       CASE WHEN i.UNIQUENESS = 'UNIQUE' THEN 1 ELSE 0 END AS \"IsUnique\", " +
                "       ic.COLUMN_NAME AS \"FieldName\", " +
                "       ic.COLUMN_POSITION AS \"KeyOrdinal\", " +
                "       CASE WHEN ic.DESCEND = 'DESC' THEN 1 ELSE 0 END AS \"IsDesc\" " +
                "FROM USER_INDEXES i " +
                "JOIN USER_IND_COLUMNS ic ON ic.INDEX_NAME = i.INDEX_NAME " +
                "LEFT JOIN USER_CONSTRAINTS c ON c.CONSTRAINT_NAME = i.INDEX_NAME AND c.CONSTRAINT_TYPE = 'P' " +
                "WHERE i.TABLE_NAME = {0} " +
                "ORDER BY (CASE WHEN c.CONSTRAINT_TYPE = 'P' THEN 0 ELSE 1 END), i.INDEX_NAME, ic.COLUMN_POSITION";
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
        /// IDENTITY flag, default expression, and column comment.
        /// </summary>
        /// <remarks>
        /// CHAR_LENGTH is used over DATA_LENGTH because the framework emits
        /// <c>VARCHAR2(n CHAR)</c> with <c>NLS_LENGTH_SEMANTICS=CHAR</c> semantics, so
        /// the character count is the meaningful length. <c>DATA_DEFAULT</c> is a LONG
        /// column on Oracle; ODP.NET reads it as a string when InitialLONGFetchSize is
        /// non-zero (the driver default). The trailing newline / whitespace Oracle
        /// appends to LONG defaults is trimmed in <see cref="ParseDBDefaultValue"/>.
        /// </remarks>
        private DataTable GetColumns(string tableName)
        {
            string sql =
                "SELECT c.COLUMN_NAME AS \"FieldName\", " +
                "       c.DATA_TYPE AS \"DbType\", " +
                "       CASE WHEN c.NULLABLE = 'Y' THEN 1 ELSE 0 END AS \"AllowDBNull\", " +
                "       CASE WHEN c.IDENTITY_COLUMN = 'YES' THEN 1 ELSE 0 END AS \"AutoIncrement\", " +
                "       COALESCE(c.CHAR_LENGTH, 0) AS \"Length\", " +
                "       COALESCE(c.DATA_PRECISION, 0) AS \"Precision\", " +
                "       COALESCE(c.DATA_SCALE, 0) AS \"Decimals\", " +
                "       COALESCE(c.DATA_DEFAULT, '') AS \"DefaultValue\", " +
                "       COALESCE(cc.COMMENTS, '') AS \"Description\" " +
                "FROM USER_TAB_COLUMNS c " +
                "LEFT JOIN USER_COL_COMMENTS cc ON cc.TABLE_NAME = c.TABLE_NAME AND cc.COLUMN_NAME = c.COLUMN_NAME " +
                "WHERE c.TABLE_NAME = {0} " +
                "ORDER BY c.COLUMN_ID";
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

            string originalDefaultValue = OracleSchemaHelper.GetDefaultValueExpression(dbField.DbType);
            dbField.DefaultValue = ParseDBDefaultValue(dataType, row.GetFieldValue<string>("DefaultValue"), originalDefaultValue);
            return dbField;
        }

        /// <summary>
        /// Converts an Oracle <c>USER_TAB_COLUMNS.DATA_TYPE</c> value to the corresponding
        /// <see cref="FieldDbType"/>.
        /// </summary>
        /// <remarks>
        /// Oracle has no native <c>BOOLEAN</c> on 19c — the framework stores booleans as
        /// <c>NUMBER(1)</c>, so a NUMBER with precision 1 and scale 0 maps back to
        /// <see cref="FieldDbType.Boolean"/>. <c>RAW(16)</c> is mapped to
        /// <see cref="FieldDbType.Guid"/> to round-trip the framework's default GUID
        /// storage. <c>NUMBER(19,4)</c> is recognised as <see cref="FieldDbType.Currency"/>;
        /// other NUMBER precisions follow the framework's emitted form (5 → Short,
        /// 10 → Integer, 19 → Long, anything else → Decimal).
        /// </remarks>
        public static FieldDbType GetFieldDbType(string dataType, int dataPrecision, int dataScale, int length)
        {
            // TIMESTAMP comes back as 'TIMESTAMP(6)' (precision in the type string itself);
            // strip any trailing parenthesised qualifier before matching.
            string type = NormalizeDataTypeName(dataType);
            switch (type)
            {
                case "varchar2":
                case "nvarchar2":
                case "char":
                case "nchar":
                    return FieldDbType.String;
                case "clob":
                case "nclob":
                case "long":
                    return FieldDbType.Text;
                case "number":
                    if (dataPrecision == 1 && dataScale == 0) return FieldDbType.Boolean;
                    if (dataPrecision == 5 && dataScale == 0) return FieldDbType.Short;
                    if (dataPrecision == 10 && dataScale == 0) return FieldDbType.Integer;
                    if (dataPrecision == 19 && dataScale == 0) return FieldDbType.Long;
                    if (dataPrecision == 19 && dataScale == 4) return FieldDbType.Currency;
                    return FieldDbType.Decimal;
                case "float":
                case "binary_float":
                case "binary_double":
                    return FieldDbType.Decimal;
                case "date":
                    return FieldDbType.Date;
                case "timestamp":
                    return FieldDbType.DateTime;
                case "raw":
                    // Framework stores GUID as RAW(16); other RAW(n) values still map to
                    // Binary because the framework's Binary type isn't sized.
                    return length == 16 ? FieldDbType.Guid : FieldDbType.Binary;
                case "blob":
                case "long raw":
                    return FieldDbType.Binary;
                default:
                    return FieldDbType.Unknown;
            }
        }

        /// <summary>
        /// Lower-cases the data-type name and strips any trailing parenthesised qualifier
        /// (e.g. <c>TIMESTAMP(6)</c> → <c>timestamp</c>, <c>TIMESTAMP(6) WITH TIME ZONE</c>
        /// → <c>timestamp with time zone</c>). The trailing space is trimmed.
        /// </summary>
        private static string NormalizeDataTypeName(string dataType)
        {
            string lower = StrFunc.ToLower(dataType ?? string.Empty).Trim();
            int parenStart = lower.IndexOf('(');
            if (parenStart < 0) return lower;
            int parenEnd = lower.IndexOf(')', parenStart);
            if (parenEnd < 0) return lower;
            string before = lower.Substring(0, parenStart);
            string after = parenEnd + 1 < lower.Length ? lower.Substring(parenEnd + 1) : string.Empty;
            return (before + after).Trim();
        }

        /// <summary>
        /// Parses the field default value retrieved from <c>USER_TAB_COLUMNS.DATA_DEFAULT</c>
        /// (a LONG column whose value Oracle returns with a trailing newline). For string
        /// types the surrounding single quotes are stripped and embedded <c>''</c> are
        /// unescaped. Returns an empty string when the parsed value matches the framework's
        /// built-in default.
        /// </summary>
        public static string ParseDBDefaultValue(string dataType, string defaultValue, string originalDefaultValue)
        {
            if (StrFunc.IsEmpty(defaultValue)) return string.Empty;

            string trimmed = defaultValue.Trim();

            string normalized;
            string typeName = NormalizeDataTypeName(dataType);
            switch (typeName)
            {
                case "varchar2":
                case "nvarchar2":
                case "char":
                case "nchar":
                case "clob":
                case "nclob":
                    normalized = StripStringLiteral(trimmed);
                    break;
                default:
                    normalized = trimmed;
                    break;
            }

            return string.Equals(originalDefaultValue, normalized, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : normalized;
        }

        /// <summary>
        /// Strips surrounding single quotes and unescapes doubled quotes for an Oracle
        /// string literal default (e.g. <c>'O''Brien'</c> → <c>O'Brien</c>).
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
