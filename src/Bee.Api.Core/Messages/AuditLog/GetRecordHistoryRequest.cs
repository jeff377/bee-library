using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API request for the get-record-history operation.
    /// </summary>
    [MessagePackObject]
    public class GetRecordHistoryRequest : ApiRequest, IGetRecordHistoryRequest
    {
        /// <summary>
        /// Gets or sets the business object (program) id whose record history is requested.
        /// </summary>
        [Key(100)]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the master record key (its <c>sys_rowid</c>).
        /// </summary>
        [Key(101)]
        public string RowKey { get; set; } = string.Empty;

        // Add new fields starting from Key(102).
    }
}
