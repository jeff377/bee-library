using Bee.Api.Contracts;
using Bee.Definition.Logging;
using MessagePack;

namespace Bee.Api.Core.Messages.AuditLog
{
    /// <summary>
    /// API response for the get-change-detail operation: one change event's header plus its restored
    /// field-level before/after values.
    /// </summary>
    [MessagePackObject]
    public class GetChangeDetailResponse : ApiResponse, IGetChangeDetailResponse
    {
        /// <summary>Gets or sets the log row's unique id (<c>st_log_change.sys_rowid</c>).</summary>
        [Key(100)]
        public Guid SysRowId { get; set; }

        /// <summary>Gets or sets the event time in UTC.</summary>
        [Key(101)]
        public DateTime LogTime { get; set; }

        /// <summary>Gets or sets the acting user's login id (denormalised).</summary>
        [Key(102)]
        public string? UserId { get; set; }

        /// <summary>Gets or sets the acting user's display name (denormalised).</summary>
        [Key(103)]
        public string? UserName { get; set; }

        /// <summary>Gets or sets the business object (program) id.</summary>
        [Key(104)]
        public string? ProgId { get; set; }

        /// <summary>Gets or sets the master record key (its <c>sys_rowid</c>).</summary>
        [Key(105)]
        public string? RowKey { get; set; }

        /// <summary>Gets or sets the change kind derived from the master row state.</summary>
        [Key(106)]
        public ChangeKind ChangeKind { get; set; }

        /// <summary>Gets or sets a value indicating whether this change touched sensitive data.</summary>
        [Key(107)]
        public bool IsSensitive { get; set; }

        /// <summary>Gets or sets the origin marker (e.g. <c>"Employee.Save"</c>).</summary>
        [Key(108)]
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the field-level before/after changes carried by this event, flattened across the
        /// master and its detail rows.
        /// </summary>
        [Key(109)]
        public List<RecordFieldChange> Fields { get; set; } = [];

        /// <inheritdoc/>
        IReadOnlyList<RecordFieldChange> IGetChangeDetailResponse.Fields => Fields;

        // Add new fields starting from Key(110).
    }
}
