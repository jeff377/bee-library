using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API request for the get-change-detail operation.
    /// </summary>
    [MessagePackObject]
    public class GetChangeDetailRequest : ApiRequest, IGetChangeDetailRequest
    {
        /// <summary>
        /// Gets or sets the change event's log row id (<c>st_log_change.sys_rowid</c>).
        /// </summary>
        [Key(100)]
        public Guid SysRowId { get; set; }

        // Add new fields starting from Key(101).
    }
}
