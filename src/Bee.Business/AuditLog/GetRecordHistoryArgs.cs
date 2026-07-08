using Bee.Api.Contracts;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Input arguments for retrieving a record's change history.
    /// </summary>
    public class GetRecordHistoryArgs : BusinessArgs, IGetRecordHistoryRequest
    {
        /// <summary>
        /// Gets or sets the business object (program) id whose record history is requested.
        /// </summary>
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the master record key (its <c>sys_rowid</c>).
        /// </summary>
        public string RowKey { get; set; } = string.Empty;
    }
}
