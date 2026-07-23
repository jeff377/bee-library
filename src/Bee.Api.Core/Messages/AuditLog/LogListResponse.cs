using System.Data;
using Bee.Api.Contracts.AuditLog;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// Shared API response for the audit-log list operations (login / access / anomaly): a page of
    /// event-header rows plus paging metadata. The <see cref="Table"/> carries whichever columns the
    /// queried axis projects.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class LogListResponse : ApiResponse, ILogListResponse
    {
        /// <summary>Gets or sets the event header rows for this page.</summary>
        public DataTable? Table { get; set; }

        /// <summary>Gets or sets the paging metadata for this page.</summary>
        public PagingInfo? Paging { get; set; }

        // Add new fields starting from Key(102).
    }
}
