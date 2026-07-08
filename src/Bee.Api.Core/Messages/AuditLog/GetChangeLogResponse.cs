using System.Data;
using Bee.Api.Contracts;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API response for the change-log list operation: a page of <c>st_log_change</c> event headers.
    /// </summary>
    [MessagePackObject]
    public class GetChangeLogResponse : ApiResponse, IGetChangeLogResponse
    {
        /// <summary>Gets or sets the change-event header rows.</summary>
        [Key(100)]
        public DataTable? Table { get; set; }

        /// <summary>Gets or sets the paging metadata for this page.</summary>
        [Key(101)]
        public PagingInfo? Paging { get; set; }

        // Add new fields starting from Key(102).
    }
}
