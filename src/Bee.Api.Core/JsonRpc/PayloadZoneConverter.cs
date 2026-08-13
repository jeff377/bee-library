using Bee.Api.Core.Messages.AuditLog;
using Bee.Api.Core.Messages.Form;
using Bee.Definition.Filters;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Applies <see cref="DateTimeZoneConverter"/> to the data a request or response payload carries.
    /// </summary>
    /// <remarks>
    /// The carriers are matched by concrete message type rather than by walking the object graph:
    /// the wire contracts are flat, and a converter has to <i>write</i> the converted copy back, which
    /// the read-only contract interfaces cannot express. A new message type carrying a
    /// <c>DataSet</c>, <c>DataTable</c> or filter is therefore not covered automatically — extend the
    /// switches here when adding one.
    ///
    /// Audit-log request bounds (<c>FromUtc</c> / <c>ToUtc</c>) are deliberately left alone: they name
    /// their own basis and are system timestamps, which ADR-032 D4 keeps in UTC end to end.
    /// </remarks>
    public static class PayloadZoneConverter
    {
        /// <summary>
        /// Replaces a request's data with UTC copies for the duration of the call, and returns a
        /// token that puts the caller's own objects back.
        /// </summary>
        /// <param name="payload">The request value; unrecognised types are ignored.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank is a no-op.</param>
        /// <remarks>
        /// The caller handed us its request object and keeps using it after the call, so the swap is
        /// undone in a <c>finally</c>. Without that the caller would find its own <c>DataSet</c>
        /// replaced by a UTC one and redraw the screen in the wrong zone.
        /// </remarks>
        public static PayloadSwap ToUtc(object? payload, string timeZoneId)
        {
            if (payload is null || string.IsNullOrWhiteSpace(timeZoneId)) { return default; }

            switch (payload)
            {
                case SaveRequest request when request.DataSet != null:
                {
                    var original = request.DataSet;
                    request.DataSet = DateTimeZoneConverter.UserToUtc(original, timeZoneId);
                    return new PayloadSwap(() => request.DataSet = original);
                }
                case GetListRequest request when request.Filter != null:
                {
                    var original = request.Filter;
                    request.Filter = ConvertFilter(original, timeZoneId, toUtc: true);
                    return new PayloadSwap(() => request.Filter = original);
                }
                default:
                    return default;
            }
        }

        /// <summary>
        /// Rewrites a response's data into the user's time zone.
        /// </summary>
        /// <param name="payload">The response value; unrecognised types are ignored.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank is a no-op.</param>
        /// <remarks>
        /// No swap token here: the response object was produced for this call, so replacing its data
        /// is the delivery, not a side effect. The <c>DataSet</c> it pointed at is still copied rather
        /// than edited, which matters in-process where that object belongs to the server.
        /// </remarks>
        public static void ToUserZone(object? payload, string timeZoneId)
        {
            if (payload is null || string.IsNullOrWhiteSpace(timeZoneId)) { return; }

            switch (payload)
            {
                case GetDataResponse response:
                    response.DataSet = DateTimeZoneConverter.UtcToUser(response.DataSet, timeZoneId);
                    break;
                case GetNewDataResponse response:
                    response.DataSet = DateTimeZoneConverter.UtcToUser(response.DataSet, timeZoneId);
                    break;
                case SaveResponse response:
                    response.DataSet = DateTimeZoneConverter.UtcToUser(response.DataSet, timeZoneId);
                    break;
                case GetListResponse response:
                    response.Table = DateTimeZoneConverter.UtcToUser(response.Table, timeZoneId);
                    break;
                case GetLookupResponse response:
                    response.Table = DateTimeZoneConverter.UtcToUser(response.Table, timeZoneId);
                    break;
                case LogListResponse response:
                    response.Table = DateTimeZoneConverter.UtcToUser(response.Table, timeZoneId);
                    break;
                case LogAggregateResponse response:
                    response.Table = DateTimeZoneConverter.UtcToUser(response.Table, timeZoneId);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Rebuilds a filter tree with its temporal values converted, leaving the caller's tree intact.
        /// </summary>
        /// <param name="node">The filter node to copy.</param>
        /// <param name="timeZoneId">The user's IANA time zone id.</param>
        /// <param name="toUtc">The direction of the shift.</param>
        private static FilterNode? ConvertFilter(FilterNode? node, string timeZoneId, bool toUtc)
        {
            switch (node)
            {
                case null:
                    return null;
                case FilterGroup group:
                {
                    var copy = new FilterGroup(group.Operator);
                    foreach (var child in group.Nodes)
                    {
                        var converted = ConvertFilter(child, timeZoneId, toUtc);
                        if (converted != null) { copy.Nodes.Add(converted); }
                    }
                    return copy;
                }
                case FilterCondition condition:
                    return new FilterCondition(
                        condition.FieldName,
                        condition.Operator,
                        DateTimeZoneConverter.ConvertFilterValue(condition.Value, timeZoneId, toUtc)!,
                        DateTimeZoneConverter.ConvertFilterValue(condition.SecondValue, timeZoneId, toUtc))
                    {
                        IgnoreIfNull = condition.IgnoreIfNull
                    };
                default:
                    return node;
            }
        }
    }
}
