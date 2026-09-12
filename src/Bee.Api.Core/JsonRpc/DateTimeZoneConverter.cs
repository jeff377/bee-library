using System.Data;
using Bee.Base.Data;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Shifts the <see cref="DateTime"/> cells of a <c>DataSet</c> between UTC and a user's time
    /// zone, leaving calendar-day columns untouched.
    /// </summary>
    /// <remarks>
    /// Which columns move is decided by the <see cref="FieldDbType"/> marker the payload already carries
    /// (ADR-031), so no <see cref="Bee.Definition.Forms.FormSchema"/> is needed and report / AnyCode results convert correctly
    /// too: <c>Date</c> never shifts (a calendar day has no instant to re-express, and shifting it
    /// would land on the wrong day), <c>DateTime</c> always does.
    ///
    /// The cell's <see cref="DateTimeKind"/> is ignored. A round-tripped value comes back as
    /// <c>Unspecified</c> on one wire and <c>Utc</c> on the other (ADR-032 D6), so branching on it
    /// would make conversion depend on the deployment's <see cref="Bee.Api.Core.Messages.PayloadFormat"/>. The direction is the
    /// caller's to state, which is what the two entry points do.
    ///
    /// Both row versions are converted. A modified row carries Original alongside Current, and the
    /// server reads Original for concurrency checks and the audit DiffGram — converting only Current
    /// would leave the two versions in different zones and silently corrupt both.
    ///
    /// A wall-clock time inside a DST fall-back overlap stands for two instants. The user-zone direction
    /// remembers which instant each such cell came from, keyed by the rows it returns, and the UTC
    /// direction sends that instant back for as long as the cell still shows the same wall-clock time.
    /// Rows the caller copies or builds itself carry no memory and resolve to standard time (ADR-032 D4).
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
        /// Returns a copy of <paramref name="table"/> with instant columns moved from UTC to the
        /// user's zone.
        /// </summary>
        /// <param name="table">The table to convert; <c>null</c> returns <c>null</c>.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank is a no-op.</param>
        public static DataTable? UtcToUser(DataTable? table, string timeZoneId)
            => Convert(table, timeZoneId, toUtc: false);

        /// <summary>
        /// Returns a copy of <paramref name="dataSet"/> with instant columns moved from the user's
        /// zone to UTC — the direction for a request leaving the client.
        /// </summary>
        /// <param name="dataSet">The data set to convert; <c>null</c> returns <c>null</c>.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank is a no-op.</param>
        public static DataSet? UserToUtc(DataSet? dataSet, string timeZoneId)
            => Convert(dataSet, timeZoneId, toUtc: true);

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
            if (toUtc) { naive = SkipSpringForwardGap(naive, zone); }
            var shifted = toUtc
                ? TimeZoneInfo.ConvertTimeToUtc(naive, zone)
                : TimeZoneInfo.ConvertTimeFromUtc(naive, zone);
            return DateTime.SpecifyKind(shifted, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Moves a local time that falls inside a spring-forward gap to the first instant that
        /// actually exists, leaving every other value untouched.
        /// </summary>
        /// <remarks>
        /// A date picker has no way to know that a wall-clock time does not exist on a given day, so
        /// a user picking 02:30 on a transition day is doing something entirely reasonable. Without
        /// this, <c>ConvertTimeToUtc</c> throws <see cref="ArgumentException"/> and that exception
        /// travels out through the JSON-RPC boundary as an opaque failure.
        /// <para>
        /// Shifting forward by the gap's own length is the convention the mainstream date pickers
        /// use (iOS, Android, Google Calendar): 02:30 on a one-hour spring-forward day becomes
        /// 03:30. The gap length is derived from the offsets either side of the transition rather
        /// than assumed to be one hour — not every zone moves by exactly an hour.
        /// </para>
        /// <para>
        /// The fall-back (ambiguous) direction does not throw: <c>ConvertTimeToUtc</c> resolves a
        /// repeated local time to standard time. A cell that came from a converted response is sent
        /// back as the instant it came from instead; see <see cref="CellShift"/>.
        /// </para>
        /// </remarks>
        private static DateTime SkipSpringForwardGap(DateTime naive, TimeZoneInfo zone)
        {
            if (!zone.IsInvalidTime(naive)) { return naive; }

            var gap = zone.GetUtcOffset(naive.Date.AddDays(1)) - zone.GetUtcOffset(naive.Date.AddDays(-1));
            return gap > TimeSpan.Zero ? naive.Add(gap) : naive;
        }

        /// <summary>
        /// Returns the shift applied to one row's instant cells.
        /// </summary>
        /// <param name="copy">The row being rewritten, which is the one handed back to the caller.</param>
        /// <param name="source">The caller's own row the copy was made from.</param>
        /// <param name="zone">The user's time zone.</param>
        /// <param name="toUtc">The direction of the shift.</param>
        /// <remarks>
        /// The user-zone direction remembers ambiguous cells against the copy, because the copy is what
        /// the caller keeps and later sends back. The UTC direction therefore looks them up against the
        /// source, which is that same row returning.
        /// </remarks>
        private static Func<DateTime, DataColumn, DataRowVersion, DateTime> CellShift(
            DataRow copy, DataRow source, TimeZoneInfo zone, bool toUtc)
        {
            if (toUtc)
            {
                return (value, column, version) =>
                    AmbiguousInstantMemory.TryRecall(source, column, version, value, zone, out var utc)
                        ? utc
                        : Shift(value, zone, toUtc: true);
            }

            return (value, column, version) =>
            {
                var local = Shift(value, zone, toUtc: false);
                AmbiguousInstantMemory.Remember(copy, column, version, value, local, zone);
                return local;
            };
        }

        private static DataSet? Convert(DataSet? dataSet, string timeZoneId, bool toUtc)
        {
            if (dataSet == null || IsNoOp(timeZoneId)) { return dataSet; }

            var zone = ResolveZone(timeZoneId);
            var copy = dataSet.Copy();
            for (int i = 0; i < copy.Tables.Count; i++)
            {
                ConvertInPlace(copy.Tables[i], dataSet.Tables[i], zone, toUtc);
            }
            return copy;
        }

        private static DataTable? Convert(DataTable? table, string timeZoneId, bool toUtc)
        {
            if (table == null || IsNoOp(timeZoneId)) { return table; }

            var copy = table.Copy();
            ConvertInPlace(copy, table, ResolveZone(timeZoneId), toUtc);
            return copy;
        }

        /// <summary>
        /// Rewrites the instant cells of an already-copied table, preserving each row's state and
        /// both of its versions.
        /// </summary>
        /// <param name="table">A table the caller owns exclusively.</param>
        /// <param name="source">The table <paramref name="table"/> was copied from; rows align by index.</param>
        /// <param name="zone">The user's time zone.</param>
        /// <param name="toUtc">The direction of the shift.</param>
        private static void ConvertInPlace(DataTable table, DataTable source, TimeZoneInfo zone, bool toUtc)
        {
            var columns = InstantColumns(table);
            if (columns.Count == 0) { return; }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var shift = CellShift(row, source.Rows[i], zone, toUtc);

                // Writing a cell always marks the row Modified, so each state needs its own recovery:
                // the value must change while the row's meaning to the server must not.
                switch (row.RowState)
                {
                    case DataRowState.Deleted:
                        // A deleted row exposes only Original, and writing to it would first have to
                        // undo the delete. The server reads it for the audit trail, so it goes through
                        // reject / rewrite / re-delete.
                        ConvertDeletedRow(row, columns, shift);
                        break;

                    case DataRowState.Modified:
                        ConvertModifiedRow(row, columns, shift);
                        break;

                    case DataRowState.Unchanged:
                        // Accept afterwards so the converted value becomes the new Original too —
                        // otherwise the row would arrive at the server looking edited.
                        WriteCurrent(row, columns, shift);
                        row.AcceptChanges();
                        break;

                    default:
                        // Added: only Current exists, and it must stay Added.
                        WriteCurrent(row, columns, shift);
                        break;
                }
            }
        }

        private static void WriteCurrent(DataRow row, List<DataColumn> columns,
            Func<DateTime, DataColumn, DataRowVersion, DateTime> shift)
        {
            foreach (var column in columns)
            {
                if (row[column] is DateTime current) { row[column] = shift(current, column, DataRowVersion.Current); }
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
            DataRowVersion version, Func<DateTime, DataColumn, DataRowVersion, DateTime> shift)
        {
            var captured = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var column in columns)
            {
                var value = row[column, version];
                captured[column.ColumnName] = value is DateTime instant ? shift(instant, column, version) : value;
            }
            return captured;
        }

        /// <summary>
        /// Converts both versions of a modified row, leaving it Modified with every edit intact.
        /// </summary>
        /// <remarks>
        /// ADO.NET offers no way to write the Original version directly. The row is therefore
        /// rejected back to Original, given the converted Original values, accepted so those become
        /// the new Original, then given its converted Current values again.
        /// <para>
        /// WARNING: <see cref="DataRow.RejectChanges"/> reverts every column, not only the instant
        /// ones, so both versions are captured across the whole row before it runs. Restoring only the
        /// instant columns silently discarded every other edit on the row, in any time zone.
        /// <c>DateTimeZoneConverterTests.Convert_ModifiedRowWithNonInstantEdit_KeepsTheEdit</c> pins this.
        /// </para>
        /// </remarks>
        private static void ConvertModifiedRow(DataRow row, List<DataColumn> instantColumns,
            Func<DateTime, DataColumn, DataRowVersion, DateTime> shift)
        {
            var original = CaptureRow(row, DataRowVersion.Original, instantColumns, shift);
            var current = CaptureRow(row, DataRowVersion.Current, instantColumns, shift);

            row.RejectChanges();
            WriteRow(row, original);
            row.AcceptChanges();
            WriteRow(row, current);

            // The server picks UPDATE from the row state alone, so a row whose two versions happen to
            // hold equal values must stay Modified even though the write above changed nothing.
            if (row.RowState == DataRowState.Unchanged) { row.SetModified(); }
        }

        private static object[] CaptureRow(DataRow row, DataRowVersion version, List<DataColumn> instantColumns,
            Func<DateTime, DataColumn, DataRowVersion, DateTime> shift)
        {
            var values = new object[row.Table.Columns.Count];
            foreach (DataColumn column in row.Table.Columns)
            {
                var value = row[column, version];
                values[column.Ordinal] = value is DateTime instant && instantColumns.Contains(column)
                    ? shift(instant, column, version)
                    : value;
            }
            return values;
        }

        /// <summary>
        /// Writes the columns whose value differs from what the row holds now.
        /// </summary>
        /// <remarks>
        /// Skipping equal values is what lets this run over the whole row: an expression column
        /// computes its own value and rejects a write, and a read-only column that did not change is
        /// never assigned, so it cannot raise <see cref="ReadOnlyException"/>.
        /// </remarks>
        private static void WriteRow(DataRow row, object[] values)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                if (column.Expression.Length > 0 || Equals(row[column], values[column.Ordinal])) { continue; }
                row[column] = values[column.Ordinal];
            }
        }

        private static void ConvertDeletedRow(DataRow row, List<DataColumn> columns,
            Func<DateTime, DataColumn, DataRowVersion, DateTime> shift)
        {
            var converted = CaptureVersion(row, columns, DataRowVersion.Original, shift);

            row.RejectChanges();
            foreach (var column in columns) { row[column] = converted[column.ColumnName] ?? DBNull.Value; }
            row.AcceptChanges();
            row.Delete();
        }
    }
}
