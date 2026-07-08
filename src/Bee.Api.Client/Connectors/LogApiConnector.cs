using Bee.Api.Core.Messages;
using Bee.Api.Core.Messages.AuditLog;
using Bee.Definition;

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
        /// Asynchronously gets a record's change history (all <c>st_log_change</c> events for a
        /// <c>progId</c> + <c>rowKey</c>), each with restored before/after field values.
        /// </summary>
        /// <param name="progId">The business object (program) id whose record history is requested.</param>
        /// <param name="rowKey">The master record key (its <c>sys_rowid</c>).</param>
        public virtual async Task<GetRecordHistoryResponse> GetRecordHistoryAsync(string progId, string rowKey)
        {
            var request = new GetRecordHistoryRequest { ProgId = progId, RowKey = rowKey };
            return await ExecuteAsync<GetRecordHistoryResponse>(LogActions.GetRecordHistory, request).ConfigureAwait(false);
        }
    }
}
