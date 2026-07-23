using System.Data;
using Bee.Api.Contracts;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API response for the change-log list operation: a page of <c>st_log_change</c> event headers.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetChangeLogResponse : ApiResponse, IGetChangeLogResponse
    {
        /// <summary>Gets or sets the change-event header rows.</summary>
        public DataTable? Table { get; set; }

        /// <summary>Gets or sets the paging metadata for this page.</summary>
        public PagingInfo? Paging { get; set; }

        // Add new fields starting from Key(102).
    }
}
