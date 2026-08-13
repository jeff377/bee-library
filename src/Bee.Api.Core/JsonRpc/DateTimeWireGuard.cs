using System.Data;
using Bee.Api.Contracts.AuditLog;
using Bee.Api.Contracts.Form;
using Bee.Definition.Filters;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Enforces the two wire invariants of ADR-032 D6 on a payload value: DateTime columns must use
    /// <see cref="DataSetDateTime.Unspecified"/>, and loose DateTime values must not carry
    /// <see cref="DateTimeKind.Local"/>.
    /// </summary>
    /// <remarks>
    /// The two invariants guard different carriers because the carriers fail differently.
    ///
    /// A <see cref="DataColumn"/> normalises away whatever <see cref="DateTimeKind"/> a caller
    /// assigns, so inspecting cell values proves nothing —— they always read back as
    /// <see cref="DateTimeKind.Unspecified"/>. What actually decides whether XML gains a time-zone
    /// offset is <see cref="DataColumn.DateTimeMode"/>, and ADO.NET's default
    /// (<see cref="DataSetDateTime.UnspecifiedLocal"/>) is the offending value.
    ///
    /// A loose DateTime has no such buffer, and <see cref="DateTimeKind.Local"/> shifts the reading
    /// on both wires —— MessagePack converts to UTC as it writes, JSON writes an offset that the
    /// reader re-applies in its own zone (possibly landing on another day).
    ///
    /// The check is deliberately targeted rather than a general reflection walk: it inspects the
    /// carriers that actually admit caller-supplied time values —— the DataSet/DataTable members of
    /// the request and response contracts, and the values inside a filter tree. A new contract that
    /// introduces a DateTime-bearing member is therefore not covered automatically; extend the
    /// switch in <see cref="Validate"/> when adding one.
    ///
    /// This runs at the Connector boundary rather than at a serializer entry point on purpose:
    /// in-process calls (<c>LocalApiProvider</c> with <c>PayloadFormat.Plain</c>) never serialize,
    /// so a serializer-level guard would leave that path unguarded.
    /// </remarks>
    public static class DateTimeWireGuard
    {
        /// <summary>
        /// Validates a request or response payload value.
        /// </summary>
        /// <param name="value">The payload value; <c>null</c> and unrecognised types are ignored.</param>
        /// <exception cref="InvalidOperationException">An invariant is violated.</exception>
        public static void Validate(object? value)
        {
            switch (value)
            {
                case null:
                    return;
                case DataSet dataSet:
                    ValidateDataSet(dataSet);
                    return;
                case DataTable table:
                    ValidateDataTable(table);
                    return;
                case IGetListRequest request:
                    ValidateFilter(request.Filter);
                    return;
                case ISaveRequest request:
                    ValidateDataSet(request.DataSet);
                    return;
                case IGetDataResponse response:
                    ValidateDataSet(response.DataSet);
                    return;
                case IGetNewDataResponse response:
                    ValidateDataSet(response.DataSet);
                    return;
                case ISaveResponse response:
                    ValidateDataSet(response.DataSet);
                    return;
                case IGetListResponse response:
                    ValidateDataTable(response.Table);
                    return;
                case IGetLookupResponse response:
                    ValidateDataTable(response.Table);
                    return;
                case ILogListResponse response:
                    ValidateDataTable(response.Table);
                    return;
                case ILogAggregateResponse response:
                    ValidateDataTable(response.Table);
                    return;
                default:
                    return;
            }
        }

        private static void ValidateDataSet(DataSet? dataSet)
        {
            if (dataSet == null) { return; }

            foreach (DataTable table in dataSet.Tables)
            {
                ValidateDataTable(table);
            }
        }

        private static void ValidateDataTable(DataTable? table)
        {
            if (table == null) { return; }

            foreach (DataColumn column in table.Columns)
            {
                if (column.DataType == typeof(DateTime) &&
                    column.DateTimeMode != DataSetDateTime.Unspecified)
                {
                    throw new InvalidOperationException(
                        $"Column '{table.TableName}.{column.ColumnName}' uses DateTimeMode " +
                        $"'{column.DateTimeMode}'. Wire payloads require DataSetDateTime.Unspecified " +
                        "so XML serialization cannot introduce a time-zone offset. Call " +
                        "DataTableExtensions.NormalizeDateTimeMode on tables built by ADO.NET. " +
                        "See docs/adr/adr-032-datetime-timezone.md (D6).");
                }
            }
        }

        private static void ValidateFilter(FilterNode? node)
        {
            switch (node)
            {
                case null:
                    return;
                case FilterGroup group:
                    foreach (var child in group.Nodes)
                    {
                        ValidateFilter(child);
                    }
                    return;
                case FilterCondition condition:
                    ValidateFilterValue(condition.FieldName, condition.Value);
                    ValidateFilterValue(condition.FieldName, condition.SecondValue);
                    return;
                default:
                    return;
            }
        }

        private static void ValidateFilterValue(string fieldName, object? value)
        {
            if (value is DateTime dateTime && dateTime.Kind == DateTimeKind.Local)
            {
                throw new InvalidOperationException(
                    $"Filter condition on '{fieldName}' carries a DateTime with Kind=Local. " +
                    "Local values shift on both wires (MessagePack converts to UTC on write, JSON " +
                    "writes an offset the reader re-applies), so only Unspecified or Utc may cross " +
                    "the wire. See docs/adr/adr-032-datetime-timezone.md (D6).");
            }

            if (value is IEnumerable<object?> items)
            {
                foreach (var item in items)
                {
                    ValidateFilterValue(fieldName, item);
                }
            }
        }
    }
}
