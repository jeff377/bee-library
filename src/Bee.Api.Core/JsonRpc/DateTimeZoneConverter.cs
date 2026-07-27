using System.Data;
using Bee.Base.Data;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Shifts the <see cref="DateTime"/> cells of a <c>DataSet</c> between UTC and a user's time
    /// zone, leaving calendar-day columns untouched.
    /// </summary>
    /// <remarks>
    /// Which columns move is decided by the <c>FieldDbType</c> marker the payload already carries
    /// (ADR-031), so no <c>FormSchema</c> is needed and report / AnyCode results convert correctly
    /// too: <c>Date</c> never shifts (a calendar day has no instant to re-express, and shifting it
    /// would land on the wrong day), <c>DateTime</c> always does.
    ///
    /// The cell's <see cref="DateTimeKind"/> is ignored. A round-tripped value comes back as
    /// <c>Unspecified</c> on one wire and <c>Utc</c> on the other (ADR-032 D6), so branching on it
    /// would make conversion depend on the deployment's <c>PayloadFormat</c>. The direction is the
    /// caller's to state, which is what the two entry points do.
    ///
    /// Both row versions are converted. A modified row carries Original alongside Current, and the
    /// server reads Original for concurrency checks and the audit DiffGram — converting only Current
    /// would leave the two versions in different zones and silently corrupt both.
    /// </remarks>
    public static class DateTimeZoneConverter
    {
        /// <summary>
        /// Returns a copy of <paramref name="dataSet"/> with instant columns moved from UTC to the
        /// user's zone — the direction for a response arriving at the client.
        /// </summary>
        /// <param name="dataSet">The data set to convert; <c>null</c> returns <c>null</c>.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank is a no-op.</param>
        public static DataSet? UtcToUser(DataSet? dataSet, string timeZoneId)
            => Convert(dataSet, timeZoneId, toUtc: false);

        /// <summary>
        /// Returns a copy of <paramref name="dataSet"/> with instant columns moved from the user's
        /// zone to UTC — the direction for a request leaving the client.
        /// </summary>
        /// <param name="dataSet">The data set to convert; <c>null</c> returns <c>null</c>.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank is a no-op.</param>
        public static DataSet? UserToUtc(DataSet? dataSet, string timeZoneId)
            => Convert(dataSet, timeZoneId, toUtc: true);

        /// <summary>
        /// Returns a copy of <paramref name="table"/> with instant columns moved from UTC to the
        /// user's zone.
        /// </summary>
        /// <param name="table">The table to convert; <c>null</c> returns <c>null</c>.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank is a no-op.</param>
        public static DataTable? UtcToUser(DataTable? table, string timeZoneId)
            => Convert(table, timeZoneId, toUtc: false);

        /// <summary>
        /// Returns a copy of <paramref name="table"/> with instant columns moved from the user's
        /// zone to UTC.
        /// </summary>
        /// <param name="table">The table to convert; <c>null</c> returns <c>null</c>.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank is a no-op.</param>
        public static DataTable? UserToUtc(DataTable? table, string timeZoneId)
            => Convert(table, timeZoneId, toUtc: true);

        /// <summary>
        /// Converts a loose filter value: a <see cref="DateTime"/> is an instant and moves, a
        /// <see cref="DateOnly"/> is a calendar day and does not.
        /// </summary>
        /// <param name="value">The filter value.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank is a no-op.</param>
        /// <param name="toUtc"><c>true</c> to move to UTC, <c>false</c> to move to the user's zone.</param>
        /// <remarks>
        /// A filter value has no <c>DataColumn</c> to carry a marker, so its own CLR type states the
        /// semantics (ADR-032 D4). Missing this conversion costs no error — the query simply returns
        /// the wrong rows around a day boundary.
        /// </remarks>
        public static object? ConvertFilterValue(object? value, string timeZoneId, bool toUtc)
        {
            if (value is not DateTime instant || IsNoOp(timeZoneId)) { return value; }
            return Shift(instant, ResolveZone(timeZoneId), toUtc);
        }

        private static bool IsNoOp(string timeZoneId) => string.IsNullOrWhiteSpace(timeZoneId);

        private static TimeZoneInfo ResolveZone(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                throw new InvalidOperationException(
                    $"Time zone '{timeZoneId}' was not found. Check the id is a valid IANA name, and " +
                    "that the runtime ships time zone data — a trimmed WASM or mobile build with " +
                    "InvariantGlobalization enabled has none. See docs/adr/adr-032-datetime-timezone.md.", ex);
            }
        }

        private static DateTime Shift(DateTime value, TimeZoneInfo zone, bool toUtc)
        {
            // SpecifyKind first: ConvertTime* rejects a value whose Kind contradicts the requested
            // direction, and the incoming Kind is not trustworthy anyway (see the type remarks).
            var naive = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            var shifted = toUtc
                ? TimeZoneInfo.ConvertTimeToUtc(naive, zone)
                : TimeZoneInfo.ConvertTimeFromUtc(naive, zone);
            return DateTime.SpecifyKind(shifted, DateTimeKind.Unspecified);
        }

        private static DataSet? Convert(DataSet? dataSet, string timeZoneId, bool toUtc)
        {
            if (dataSet == null || IsNoOp(timeZoneId)) { return dataSet; }

            var zone = ResolveZone(timeZoneId);
            var copy = dataSet.Copy();
            foreach (DataTable table in copy.Tables)
            {
                ConvertInPlace(table, zone, toUtc);
            }
            return copy;
        }

        private static DataTable? Convert(DataTable? table, string timeZoneId, bool toUtc)
        {
            if (table == null || IsNoOp(timeZoneId)) { return table; }

            var copy = table.Copy();
            ConvertInPlace(copy, ResolveZone(timeZoneId), toUtc);
            return copy;
        }

        /// <summary>
        /// Rewrites the instant cells of an already-copied table, preserving each row's state and
        /// both of its versions.
        /// </summary>
        /// <param name="table">A table the caller owns exclusively.</param>
        /// <param name="zone">The user's time zone.</param>
        /// <param name="toUtc">The direction of the shift.</param>
        private static void ConvertInPlace(DataTable table, TimeZoneInfo zone, bool toUtc)
        {
            var columns = InstantColumns(table);
            if (columns.Count == 0) { return; }

            foreach (DataRow row in table.Rows)
            {
                // Writing a cell always marks the row Modified, so each state needs its own recovery:
                // the value must change while the row's meaning to the server must not.
                switch (row.RowState)
                {
                    case DataRowState.Deleted:
                        // A deleted row exposes only Original, and writing to it would first have to
                        // undo the delete. The server reads it for the audit trail, so it goes through
                        // reject / rewrite / re-delete.
                        ConvertDeletedRow(row, columns, zone, toUtc);
                        break;

                    case DataRowState.Modified:
                        var original = CaptureVersion(row, columns, DataRowVersion.Original, zone, toUtc);
                        WriteCurrent(row, columns, zone, toUtc);
                        RewriteOriginal(row, columns, original);
                        break;

                    case DataRowState.Unchanged:
                        // Accept afterwards so the converted value becomes the new Original too —
                        // otherwise the row would arrive at the server looking edited.
                        WriteCurrent(row, columns, zone, toUtc);
                        row.AcceptChanges();
                        break;

                    default:
                        // Added: only Current exists, and it must stay Added.
                        WriteCurrent(row, columns, zone, toUtc);
                        break;
                }
            }
        }

        private static void WriteCurrent(DataRow row, List<DataColumn> columns, TimeZoneInfo zone, bool toUtc)
        {
            foreach (var column in columns)
            {
                if (row[column] is DateTime current) { row[column] = Shift(current, zone, toUtc); }
            }
        }

        private static List<DataColumn> InstantColumns(DataTable table)
        {
            var columns = new List<DataColumn>();
            foreach (DataColumn column in table.Columns)
            {
                if (column.DataType == typeof(DateTime) &&
                    column.ResolveFieldDbType() != FieldDbType.Date)
                {
                    columns.Add(column);
                }
            }
            return columns;
        }

        private static Dictionary<string, object?> CaptureVersion(DataRow row, List<DataColumn> columns,
            DataRowVersion version, TimeZoneInfo zone, bool toUtc)
        {
            var captured = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var column in columns)
            {
                var value = row[column, version];
                captured[column.ColumnName] = value is DateTime instant ? Shift(instant, zone, toUtc) : value;
            }
            return captured;
        }

        /// <summary>
        /// Replaces a modified row's Original values with the converted ones, leaving it Modified.
        /// </summary>
        /// <remarks>
        /// ADO.NET offers no way to write the Original version directly. The row is therefore
        /// rejected back to Original, rewritten, accepted so those values become the new Original,
        /// then given its Current values again — which returns it to Modified with both versions in
        /// the target zone.
        /// </remarks>
        private static void RewriteOriginal(DataRow row, List<DataColumn> columns,
            Dictionary<string, object?> convertedOriginal)
        {
            var current = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var column in columns) { current[column.ColumnName] = row[column]; }

            row.RejectChanges();
            foreach (var column in columns) { row[column] = convertedOriginal[column.ColumnName] ?? DBNull.Value; }
            row.AcceptChanges();
            foreach (var column in columns) { row[column] = current[column.ColumnName] ?? DBNull.Value; }
        }

        private static void ConvertDeletedRow(DataRow row, List<DataColumn> columns, TimeZoneInfo zone, bool toUtc)
        {
            var converted = CaptureVersion(row, columns, DataRowVersion.Original, zone, toUtc);

            row.RejectChanges();
            foreach (var column in columns) { row[column] = converted[column.ColumnName] ?? DBNull.Value; }
            row.AcceptChanges();
            row.Delete();
        }
    }
}
