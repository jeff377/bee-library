using System.Globalization;
using Bee.Base;
using Bee.Base.Data;
using Bee.Db;
using Bee.Definition.Database;
using Bee.Definition.Storage;

namespace Bee.LoadTests.Bootstrap
{
    /// <summary>
    /// Plants rows into a table so read scenarios have something to read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rows are written with direct INSERTs rather than through the framework's own save path.
    /// Seeding is not what a run measures; going through business objects would cost orders of
    /// magnitude more time and would also fill the audit trail with change records that say
    /// nothing about the workload.
    /// </para>
    /// <para>
    /// Values are derived from the row index, not randomly, so two runs seeded the same way hold
    /// the same data and their numbers can be compared. Table and column identifiers come from the
    /// definition files rather than from user input; every value is passed as a parameter.
    /// </para>
    /// </remarks>
    public static class DataSeeder
    {
        // A fixed base date keeps seeded temporal values identical between runs.
        private static readonly DateTime s_baseDate = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Ensures a table holds at least <paramref name="rowCount"/> rows, inserting the
        /// shortfall.
        /// </summary>
        /// <param name="defineAccess">Definition access, for the table schema.</param>
        /// <param name="dbAccess">Database access for the table's category.</param>
        /// <param name="categoryId">The category the table belongs to.</param>
        /// <param name="tableName">The table to seed.</param>
        /// <param name="rowCount">The row count to reach.</param>
        /// <returns>The number of rows inserted; zero when the table already held enough.</returns>
        public static int EnsureRows(
            IDefineAccess defineAccess,
            DbAccess dbAccess,
            string categoryId,
            string tableName,
            int rowCount)
        {
            ArgumentNullException.ThrowIfNull(defineAccess);
            ArgumentNullException.ThrowIfNull(dbAccess);
            ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

            var schema = defineAccess.GetTableSchema(categoryId, tableName);
            var fields = schema.Fields;
            if (fields is null || fields.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Table schema '{categoryId}/{tableName}' declares no fields.");
            }

            var existing = CountRows(dbAccess, tableName);
            if (existing >= rowCount) { return 0; }

            // Auto-increment columns are assigned by the engine, so they are left out of the
            // column list entirely rather than given a value.
            var writable = fields
                .Where(field => field.DbType != FieldDbType.AutoIncrement)
                .ToArray();

            var columns = string.Join(", ", writable.Select(field => field.FieldName));
            var placeholders = string.Join(", ",
                Enumerable.Range(0, writable.Length).Select(i => "{" + i.ToString(CultureInfo.InvariantCulture) + "}"));
            var commandText = $"INSERT INTO {tableName} ({columns}) VALUES ({placeholders})";

            var inserted = 0;
            for (int index = existing; index < rowCount; index++)
            {
                var values = writable.Select(field => CreateValue(field, index)).ToArray();
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, commandText, values));
                inserted++;
            }
            return inserted;
        }

        /// <summary>
        /// Counts the rows currently in a table.
        /// </summary>
        /// <param name="dbAccess">Database access.</param>
        /// <param name="tableName">The table name, from a definition file.</param>
        /// <returns>The row count.</returns>
        public static int CountRows(DbAccess dbAccess, string tableName)
        {
            ArgumentNullException.ThrowIfNull(dbAccess);
            var spec = new DbCommandSpec(DbCommandKind.Scalar, $"SELECT COUNT(*) FROM {tableName}");
            return Convert.ToInt32(dbAccess.Execute(spec).Scalar, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Reads a sample of row keys from a table, for scenarios that read by key.
        /// </summary>
        /// <param name="dbAccess">Database access.</param>
        /// <param name="tableName">The table name, from a definition file.</param>
        /// <param name="count">How many keys to take.</param>
        /// <returns>The keys found, which may be fewer than requested.</returns>
        public static IReadOnlyList<Guid> ReadRowIds(DbAccess dbAccess, string tableName, int count)
        {
            ArgumentNullException.ThrowIfNull(dbAccess);
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

            var spec = new DbCommandSpec(DbCommandKind.DataTable,
                $"SELECT sys_rowid FROM {tableName}");
            var table = dbAccess.Execute(spec).Table;
            if (table is null) { return []; }

            var rowIds = new List<Guid>(Math.Min(count, table.Rows.Count));
            foreach (System.Data.DataRow row in table.Rows)
            {
                if (rowIds.Count >= count) { break; }
                // Coerced rather than type-tested: Oracle returns RAW(16) as byte[], and a
                // direct `is Guid` collected nothing there — the run then aborted with
                // "No keys were collected" before any scenario ran.
                var rowId = ValueUtilities.CGuid(row[0]);
                if (rowId != Guid.Empty) { rowIds.Add(rowId); }
            }
            return rowIds;
        }

        /// <summary>
        /// Produces a deterministic value for a field at a given row index.
        /// </summary>
        /// <param name="field">The column definition.</param>
        /// <param name="index">The zero-based row index.</param>
        /// <returns>The value to insert.</returns>
        internal static object CreateValue(DbField field, int index)
        {
            ArgumentNullException.ThrowIfNull(field);

            return field.DbType switch
            {
                FieldDbType.String => CreateString(field, index),
                FieldDbType.Text => $"Load test row {index}.",
                FieldDbType.Boolean => index % 2 == 0,
                FieldDbType.Short => (short)(index % short.MaxValue),
                FieldDbType.Integer => index,
                FieldDbType.Long => (long)index,
                FieldDbType.Decimal or FieldDbType.Currency => CreateDecimal(field, index),
                FieldDbType.Date => s_baseDate.AddDays(index % 3650).Date,
                FieldDbType.DateTime => s_baseDate.AddMinutes(index),
                FieldDbType.Guid => CreateDeterministicGuid(index, field.FieldName),
                FieldDbType.Binary => BitConverter.GetBytes(index),
                // Unknown and AutoIncrement have no meaningful value to write. AutoIncrement is
                // filtered out before this point; Unknown falls back to an empty string, which is
                // what the framework's own "no default" convention writes for text columns.
                _ => string.Empty
            };
        }

        private static string CreateString(DbField field, int index)
        {
            var value = $"{field.FieldName}-{index.ToString(CultureInfo.InvariantCulture)}";
            return field.Length > 0 && value.Length > field.Length
                ? value[..field.Length]
                : value;
        }

        private static decimal CreateDecimal(DbField field, int index)
        {
            // Stay inside the column's declared scale so the value survives the round trip
            // unchanged; a value with more decimals than the column holds would be rounded by the
            // engine and no longer match what was written.
            var raw = index + 0.5m;
            return decimal.Round(raw, Math.Min(field.Scale, 2), MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Builds a GUID from the row index and column name, so seeded keys are stable across
        /// runs while still differing between columns.
        /// </summary>
        private static Guid CreateDeterministicGuid(int index, string fieldName)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(index).CopyTo(bytes, 0);
            BitConverter.GetBytes(fieldName.GetHashCode(StringComparison.Ordinal)).CopyTo(bytes, 4);
            BitConverter.GetBytes((long)index).CopyTo(bytes, 8);
            return new Guid(bytes);
        }
    }
}
