using Bee.Api.Contracts.AuditLog;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Input arguments for retrieving one change event's restored before/after detail.
    /// </summary>
    public class GetChangeDetailArgs : BusinessArgs, IGetChangeDetailRequest
    {
        /// <summary>
        /// Gets or sets the change event's log row id (<c>st_log_change.sys_rowid</c>).
        /// </summary>
        public Guid SysRowId { get; set; }
    }
}
