using System.Data;
using Bee.Base;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Sorting;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// Provides methods for reading and parsing SQLite table schemas via the
    /// <c>sqlite_master</c> table and <c>PRAGMA</c> statements. Counterpart to
    /// <see cref="PostgreSql.PgTableSchemaProvider"/>; SQLite has no schema concept
    /// and no native COMMENT facility, so descriptions are always returned as empty.
    /// </summary>
    /// <remarks>
    /// SQLite auto-generates the primary key's backing index as <c>sqlite_autoindex_*</c>,
    /// which would not match the schema's canonical <c>pk_{table}</c> naming used by
    /// <see cref="Schema.TableSchemaComparer"/>. To keep the comparer's name-based matching
    /// working without a Comparer change, the parsed PK index is exposed under the framework
    /// convention <c>pk_{table}</c>.
    /// </remarks>
    public class SqliteTableSchemaProvider : ITableSchemaProvider
    {
        private readonly DbAccess _dbAccess;

        /// <summary>
        /// Initializes a new instance of <see cref="SqliteTableSchemaProvider"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public SqliteTableSchemaProvider(string databaseId)
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
            // SQLite has no native description metadata; DisplayName is always empty.

            var columns = ReadColumns(tableName);
            ParsePrimaryKey(dbTable, columns, tableName);
            ParseIndexes(dbTable, tableName);

            foreach (DataRow row in columns.Rows)
            {
                dbTable.Fields!.Add(ParseDbField(row));
            }

            return dbTable;
        }

        /// <summary>
        /// Determines whether the specified table exists in the database.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        private bool TableExists(string tableName)
        {
            const string sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name={0}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, tableName);
            var result = _dbAccess.Execute(command);
            return BaseFunc.CInt(result.Scalar!) > 0;
        }

        /// <summary>
        /// Returns the columns of the specified table via <c>PRAGMA table_info</c>.
        /// Columns: cid, name, type, notnull, dflt_value, pk.
        /// </summary>
        /// <remarks>
        /// PRAGMA result columns are dynamically typed: <c>dflt_value</c> is null for most
        /// rows (in which case Microsoft.Data.Sqlite reports the column as BLOB) but a string
        /// for rows that have a default. <see cref="DataTable.Load(IDataReader)"/>'s schema inference would
        /// fail when a later row carries a string into a column it inferred as BLOB, so we
        /// load via a reader and copy each cell as <see cref="object"/> instead.
        /// </remarks>
        private DataTable ReadColumns(string tableName)
        {
            string sql = $"PRAGMA table_info({SqliteSchemaSyntax.QuoteName(tableName)})";
            return ReadDynamicPragma(sql, "Columns");
        }

        /// <summary>
        /// Executes a PRAGMA-like statement and returns the result as a DataTable whose
        /// columns are all typed <see cref="object"/>. This avoids the heterogeneous-typing
        /// failure that <see cref="DataTable.Load(IDataReader)"/> hits on PRAGMA result sets.
        /// </summary>
        private DataTable ReadDynamicPragma(string sql, string resultTableName)
        {
            var connInfo = DbConnectionManager.GetConnectionInfo(DatabaseId);
            using var conn = connInfo.Provider.CreateConnection()
                ?? throw new InvalidOperationException("Provider returned a null DbConnection.");
            conn.ConnectionString = connInfo.ConnectionString;
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();

            var table = new DataTable(resultTableName);
            for (int i = 0; i < reader.FieldCount; i++)
                table.Columns.Add(reader.GetName(i), typeof(object));

            while (reader.Read())
            {
                var row = table.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                table.Rows.Add(row);
            }
            return table;
        }

        /// <summary>
        /// Builds the primary key index from the column data (each column row carries a non-zero
        /// <c>pk</c> ordinal when it participates in the primary key).
        /// </summary>
        private static void ParsePrimaryKey(TableSchema dbTable, DataTable columns, string tableName)
        {
            var pkColumns = columns.AsEnumerable()
                .Where(r => BaseFunc.CInt(r["pk"]) > 0)
                .OrderBy(r => BaseFunc.CInt(r["pk"]))
                .ToList();

            if (pkColumns.Count == 0) return;

            // Expose under the framework-convention name pk_{table} so name-based comparison matches.
            var pkIndex = new TableSchemaIndex
            {
                Name = $"pk_{tableName}",
                PrimaryKey = true,
                Unique = true
            };
            foreach (var row in pkColumns)
            {
                pkIndex.IndexFields!.Add(new IndexField
                {
                    FieldName = BaseFunc.CStr(row["name"]),
                    SortDirection = SortDirection.Asc
                });
            }
            dbTable.Indexes!.Add(pkIndex);
        }

        /// <summary>
        /// Reads non-PK indexes via <c>PRAGMA index_list</c> + <c>PRAGMA index_info</c>.
        /// PK-backed and PK-by-rowid auto-indexes (origin = "pk") are skipped because the PK
        /// is already populated by <see cref="ParsePrimaryKey"/>.
        /// </summary>
        private void ParseIndexes(TableSchema dbTable, string tableName)
        {
            string listSql = $"PRAGMA index_list({SqliteSchemaSyntax.QuoteName(tableName)})";
            var listResult = ReadDynamicPragma(listSql, "IndexList");

            foreach (DataRow row in listResult.Rows)
            {
                string origin = BaseFunc.CStr(row["origin"]);
                if (StringUtilities.IsEquals(origin, "pk")) continue;

                string indexName = BaseFunc.CStr(row["name"]);
                bool unique = BaseFunc.CBool(row["unique"]);

                var indexFields = ReadIndexFields(indexName);
                if (indexFields.Count == 0) continue;

                var index = new TableSchemaIndex
                {
                    Name = indexName,
                    Unique = unique
                };
                foreach (var fieldName in indexFields)
                {
                    index.IndexFields!.Add(new IndexField
                    {
                        FieldName = fieldName,
                        SortDirection = SortDirection.Asc
                    });
                }
                dbTable.Indexes!.Add(index);
            }
        }

        /// <summary>
        /// Reads the column names of the specified index via <c>PRAGMA index_info</c>.
        /// </summary>
        private List<string> ReadIndexFields(string indexName)
        {
            string sql = $"PRAGMA index_info({SqliteSchemaSyntax.QuoteName(indexName)})";
            var result = ReadDynamicPragma(sql, "IndexInfo");
            var fields = new List<string>();
            foreach (DataRow row in result.Rows)
                fields.Add(BaseFunc.CStr(row["name"]));
            return fields;
        }

        /// <summary>
        /// Creates a field definition from a <c>PRAGMA table_info</c> row.
        /// </summary>
        private static DbField ParseDbField(DataRow row)
        {
            string columnName = BaseFunc.CStr(row["name"]);
            string declaredType = BaseFunc.CStr(row["type"]);
            bool notNull = BaseFunc.CInt(row["notnull"]) != 0;
            bool isPrimaryKey = BaseFunc.CInt(row["pk"]) > 0;

            // SQLite reports the declared type verbatim. Extract a length / precision / scale
            // hint from forms like "VARCHAR(50)" or "NUMERIC(18,2)".
            ParseTypeFacets(declaredType, out string baseType, out int length, out int precision, out int scale);

            var dbField = new DbField
            {
                FieldName = columnName,
                Caption = string.Empty,
                AllowNull = !notNull,
                DbType = MapToFieldDbType(baseType, isPrimaryKey)
            };

            if (dbField.DbType == FieldDbType.String && length > 0)
                dbField.Length = length;
            if (dbField.DbType == FieldDbType.Decimal)
            {
                dbField.Precision = precision > 0 ? precision : dbField.Precision;
                dbField.Scale = scale > 0 ? scale : dbField.Scale;
            }

            string originalDefault = SqliteSchemaSyntax.GetDefaultValueExpression(dbField.DbType);
            string raw = BaseFunc.CStr(row["dflt_value"]);
            dbField.DefaultValue = ParseDefaultValue(raw, dbField.DbType, originalDefault);
            return dbField;
        }

        /// <summary>
        /// Splits a SQLite declared type like <c>VARCHAR(50)</c> or <c>NUMERIC(18,2)</c> into its
        /// base type and any optional length/precision/scale facets.
        /// </summary>
        private static void ParseTypeFacets(string declaredType, out string baseType, out int length, out int precision, out int scale)
        {
            baseType = declaredType ?? string.Empty;
            length = 0;
            precision = 0;
            scale = 0;

            if (string.IsNullOrEmpty(baseType)) return;

            int open = baseType.IndexOf('(');
            int close = baseType.IndexOf(')');
            if (open <= 0 || close <= open) return;

            string facets = baseType.Substring(open + 1, close - open - 1);
            baseType = baseType.Substring(0, open).Trim();

            var parts = facets.Split(',');
            if (parts.Length == 1)
            {
                _ = int.TryParse(parts[0].Trim(), out length);
                _ = int.TryParse(parts[0].Trim(), out precision);
            }
            else if (parts.Length >= 2)
            {
                _ = int.TryParse(parts[0].Trim(), out precision);
                _ = int.TryParse(parts[1].Trim(), out scale);
            }
        }

        /// <summary>
        /// Maps a SQLite declared base type to the framework's <see cref="FieldDbType"/>.
        /// AutoIncrement is detected by the convention that an INTEGER column which is the
        /// primary key on SQLite is the rowid alias and behaves as auto-increment.
        /// </summary>
        public static FieldDbType MapToFieldDbType(string baseType, bool isPrimaryKey)
        {
            string normalized = (baseType ?? string.Empty).ToUpper();
            switch (normalized)
            {
                case "VARCHAR":
                case "CHAR":
                case "CHARACTER":
                case "NVARCHAR":
                    return FieldDbType.String;
                case "TEXT":
                case "CLOB":
                    return FieldDbType.Text;
                case "BOOLEAN":
                case "BOOL":
                    return FieldDbType.Boolean;
                case "SMALLINT":
                case "INT2":
                    return FieldDbType.Short;
                case "INTEGER":
                case "INT":
                case "INT4":
                    return isPrimaryKey ? FieldDbType.AutoIncrement : FieldDbType.Integer;
                case "BIGINT":
                case "INT8":
                    return FieldDbType.Long;
                case "NUMERIC":
                case "DECIMAL":
                    return FieldDbType.Decimal;
                case "REAL":
                case "DOUBLE":
                case "FLOAT":
                    return FieldDbType.Decimal;
                case "DATE":
                    return FieldDbType.Date;
                case "DATETIME":
                case "TIMESTAMP":
                    return FieldDbType.DateTime;
                case "UUID":
                    return FieldDbType.Guid;
                case "BLOB":
                case "BINARY":
                    return FieldDbType.Binary;
                default:
                    return FieldDbType.Unknown;
            }
        }

        /// <summary>
        /// Normalises the <c>dflt_value</c> raw text returned by <c>PRAGMA table_info</c>:
        /// strips surrounding parentheses (SQLite often wraps function defaults) and outer
        /// single quotes; returns an empty string when it equals the framework built-in default.
        /// </summary>
        public static string ParseDefaultValue(string rawDefault, FieldDbType dbType, string originalDefault)
        {
            if (StringUtilities.IsEmpty(rawDefault)) return string.Empty;

            string trimmed = rawDefault.Trim();

            // Unwrap an outer pair of parentheses: "(hex(randomblob(16)))" → "hex(randomblob(16))".
            if (trimmed.StartsWith('(') && trimmed.EndsWith(')'))
                trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();

            // For string types, strip the surrounding single quotes and unescape doubled quotes.
            if ((dbType == FieldDbType.String || dbType == FieldDbType.Text) &&
                trimmed.Length >= 2 && trimmed.StartsWith('\'') && trimmed.EndsWith('\''))
                trimmed = trimmed.Substring(1, trimmed.Length - 2).Replace("''", "'");

            return StringUtilities.IsEquals(originalDefault, trimmed) ? string.Empty : trimmed;
        }
    }
}
