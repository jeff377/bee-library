using Bee.Api.Core.Messages.AuditLog;
using Bee.Api.Core.Messages.Form;
using Bee.Definition.Filters;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Prepares the data a Connector call carries: a request's data is isolated from the caller and
    /// its filter values are moved to UTC, and a response's data is moved into the user's time zone.
    /// </summary>
    /// <remarks>
    /// The carriers are matched by concrete message type rather than by walking the object graph:
    /// the wire contracts are flat, and a converter has to <i>write</i> the copy back, which the
    /// read-only contract interfaces cannot express. A new message type carrying a <c>DataSet</c>,
    /// <c>DataTable</c> or filter is therefore not covered automatically — extend the switches here
    /// when adding one.
    ///
    /// On the way out only filter values are converted (ADR-032 D4). A data set in a request keeps the
    /// values the caller holds, because the server does not take <c>DateTime</c> values from a save.
    /// Strongly typed DTO properties and <c>Parameters</c> are UTC in both directions and are the
    /// caller's responsibility, which is why audit-log request bounds (<c>FromUtc</c> / <c>ToUtc</c>)
    /// are left alone.
    /// </remarks>
    public static class PayloadZoneConverter
    {
        /// <summary>
        /// Replaces a request's data with copies for the duration of the call, with filter values moved
        /// from the user's zone to UTC, and returns a token that puts the caller's own objects back.
        /// </summary>
        /// <param name="payload">The request value; unrecognised types are ignored.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank leaves filter values as they are.</param>
        /// <remarks>
        /// <para>
        /// A data set is copied whether or not there is a zone to convert to. An in-process call
        /// (<c>LocalApiProvider</c> with <c>Plain</c>) hands the server the very object it was given,
        /// and the server rewrites a saved data set in place: its <c>DateTime</c> values first, then
        /// its row states once the rows are written. Without the copy both would land in the caller's
        /// own <c>DataSet</c>.
        /// </para>
        /// <para>
        /// The caller keeps using its request object after the call, so the swap is undone in a
        /// <c>finally</c>. Without that the caller would find its own <c>DataSet</c> or filter replaced.
        /// </para>
        /// </remarks>
        public static PayloadSwap IsolateRequest(object? payload, string timeZoneId)
        {
            switch (payload)
            {
                case SaveRequest request when request.DataSet != null:
                {
                    var original = request.DataSet;
                    request.DataSet = original.Copy();
                    return new PayloadSwap(() => request.DataSet = original);
                }
                case GetListRequest request when request.Filter != null && !string.IsNullOrWhiteSpace(timeZoneId):
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
                case GetChangeDetailResponse response:
                    response.DataSet = DateTimeZoneConverter.UtcToUser(response.DataSet, timeZoneId);
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
