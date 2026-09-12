using System.Data;
using System.Runtime.CompilerServices;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Remembers which UTC instant a converted response cell came from when its wall-clock time falls
    /// in a DST fall-back overlap, so the request direction can send that same instant back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inside the overlap two UTC values share one wall-clock time. On 2026-11-01 in New York, 05:30Z
    /// and 06:30Z both read 01:30, and converting 01:30 back can only pick one of them. The information
    /// is gone the moment the response is converted, so nothing downstream can recover it: without this,
    /// saving a row after editing some other column moved the untouched instant by the overlap's length.
    /// </para>
    /// <para>
    /// Entries are keyed by the <see cref="DataRow"/> instances handed to the caller, through a
    /// <see cref="ConditionalWeakTable{TKey, TValue}"/>, and nothing is added to the data itself. Table
    /// <c>ExtendedProperties</c> were ruled out: they follow every <c>Copy</c>, <c>Merge</c> and
    /// <c>GetChanges</c> by reference, so they would reach the server on an in-process call and be
    /// written into the audit DiffGram's schema. The cost of keying by instance is that a caller who
    /// copies or rebuilds the rows gets standard time again, which is the behaviour without this type.
    /// </para>
    /// <para>
    /// A remembered instant is used only while the cell still shows the wall-clock time it was converted
    /// to. A user who picks a time inside the overlap is asking for a new value, and that resolves to
    /// standard time the way <see cref="TimeZoneInfo.ConvertTimeToUtc(DateTime, TimeZoneInfo)"/> does.
    /// </para>
    /// </remarks>
    internal static class AmbiguousInstantMemory
    {
        private static readonly ConditionalWeakTable<DataRow, Dictionary<(string Column, DataRowVersion Version), DateTime>> s_rows = [];

        /// <summary>
        /// Records the instant behind a converted cell when its wall-clock time is ambiguous.
        /// </summary>
        /// <param name="row">The row handed to the caller.</param>
        /// <param name="column">The instant column.</param>
        /// <param name="version">The row version the value was written to.</param>
        /// <param name="utc">The value before conversion.</param>
        /// <param name="local">The value after conversion.</param>
        /// <param name="zone">The user's time zone.</param>
        public static void Remember(DataRow row, DataColumn column, DataRowVersion version,
            DateTime utc, DateTime local, TimeZoneInfo zone)
        {
            if (!zone.IsAmbiguousTime(local)) { return; }

            s_rows.GetOrCreateValue(row)[(column.ColumnName, version)] =
                DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Returns the instant remembered for a cell, provided the cell still shows the wall-clock time
        /// that instant was converted to.
        /// </summary>
        /// <param name="row">The caller's row.</param>
        /// <param name="column">The instant column.</param>
        /// <param name="version">The row version being converted.</param>
        /// <param name="local">The cell's current wall-clock value in that version.</param>
        /// <param name="zone">The user's time zone.</param>
        /// <param name="utc">The remembered instant, when one applies.</param>
        public static bool TryRecall(DataRow row, DataColumn column, DataRowVersion version,
            DateTime local, TimeZoneInfo zone, out DateTime utc)
        {
            utc = default;
            if (!s_rows.TryGetValue(row, out var cells)) { return false; }

            // A row that arrived Unchanged was remembered under Current only, and that value is also
            // the Original of any edit the caller has made to the row since.
            if (!cells.TryGetValue((column.ColumnName, version), out var remembered) &&
                !(version == DataRowVersion.Original &&
                  cells.TryGetValue((column.ColumnName, DataRowVersion.Current), out remembered)))
            {
                return false;
            }

            if (TimeZoneInfo.ConvertTimeFromUtc(remembered, zone) != local) { return false; }

            utc = remembered;
            return true;
        }
    }
}
