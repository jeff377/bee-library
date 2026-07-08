using Bee.Api.Core.Messages;
using Bee.Api.Core.Messages.AuditLog;
using Bee.Definition;
using Bee.Definition.Paging;

namespace Bee.Api.Client.Connectors
{
    /// <summary>
    /// Audit-log API service connector (<c>AuditLog</c> axis): read-only queries over the
    /// <c>st_log_*</c> audit tables.
    /// </summary>
    public class LogApiConnector : ApiConnector
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="LogApiConnector"/> class using a local connection.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public LogApiConnector(Guid accessToken) : base(accessToken)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogApiConnector"/> class using a remote connection.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="accessToken">The access token.</param>
        public LogApiConnector(string endpoint, Guid accessToken) : base(endpoint, accessToken)
        { }

        #endregion

        /// <summary>
        /// Asynchronously executes an API method on the <c>AuditLog</c> business object.
        /// </summary>
        /// <param name="action">The action name to execute.</param>
        /// <param name="value">The input parameter for the action.</param>
        /// <param name="format">The payload encoding format for transmission.</param>
        public async Task<T> ExecuteAsync<T>(string action, object value, PayloadFormat format = PayloadFormat.Encrypted)
        {
            return await base.ExecuteAsync<T>(SysProgIds.AuditLog, action, value, format).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously gets a page of one record's change-event headers (all <c>st_log_change</c>
        /// events for a <c>progId</c> + <c>rowKey</c>, newest first). Fetch a single event's before/after
        /// detail with <see cref="GetChangeDetailAsync"/>.
        /// </summary>
        /// <param name="progId">The business object (program) id whose record history is requested.</param>
        /// <param name="rowKey">The master record key (its <c>sys_rowid</c>).</param>
        /// <param name="paging">The paging request; <c>null</c> applies the server default page.</param>
        public virtual async Task<GetRecordHistoryResponse> GetRecordHistoryAsync(string progId, string rowKey, PagingOptions? paging = null)
        {
            var request = new GetRecordHistoryRequest { ProgId = progId, RowKey = rowKey, Paging = paging };
            return await ExecuteAsync<GetRecordHistoryResponse>(LogActions.GetRecordHistory, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously gets a filtered, paged list of <c>st_log_change</c> event headers across records.
        /// </summary>
        /// <param name="request">The change-log list request (typed filter + optional paging).</param>
        public virtual async Task<GetChangeLogResponse> GetChangeLogAsync(GetChangeLogRequest request)
        {
            return await ExecuteAsync<GetChangeLogResponse>(LogActions.GetChangeLog, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously gets one change event's restored field-level before/after detail, by its log
        /// row id (<c>st_log_change.sys_rowid</c>).
        /// </summary>
        /// <param name="sysRowId">The change event's log row id.</param>
        public virtual async Task<GetChangeDetailResponse> GetChangeDetailAsync(Guid sysRowId)
        {
            var request = new GetChangeDetailRequest { SysRowId = sysRowId };
            return await ExecuteAsync<GetChangeDetailResponse>(LogActions.GetChangeDetail, request).ConfigureAwait(false);
        }
    }
}
