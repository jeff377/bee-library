using Bee.Api.Contracts.AuditLog;
using Bee.Definition.Logging;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Output result for one change event's restored before/after detail.
    /// </summary>
    public class GetChangeDetailResult : BusinessResult, IGetChangeDetailResponse
    {
        /// <summary>Gets or sets the log row's unique id (<c>st_log_change.sys_rowid</c>).</summary>
        public Guid SysRowId { get; set; }

        /// <summary>Gets or sets the event time in UTC.</summary>
        public DateTime LogTime { get; set; }

        /// <summary>Gets or sets the acting user's login id (denormalised).</summary>
        public string? UserId { get; set; }

        /// <summary>Gets or sets the acting user's display name (denormalised).</summary>
        public string? UserName { get; set; }

        /// <summary>Gets or sets the business object (program) id.</summary>
        public string? ProgId { get; set; }

        /// <summary>Gets or sets the master record key (its <c>sys_rowid</c>).</summary>
        public string? RowKey { get; set; }

        /// <summary>Gets or sets the change kind derived from the master row state.</summary>
        public ChangeKind ChangeKind { get; set; }

        /// <summary>Gets or sets a value indicating whether this change touched sensitive data.</summary>
        public bool IsSensitive { get; set; }

        /// <summary>Gets or sets the origin marker (e.g. <c>"Employee.Save"</c>).</summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the field-level before/after changes carried by this event, flattened across the
        /// master and its detail rows.
        /// </summary>
        public List<RecordFieldChange> Fields { get; set; } = [];

        /// <inheritdoc/>
        IReadOnlyList<RecordFieldChange> IGetChangeDetailResponse.Fields => Fields;
    }
}
