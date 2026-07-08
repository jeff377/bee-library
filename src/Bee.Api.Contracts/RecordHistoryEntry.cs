using Bee.Definition.Logging;
using MessagePack;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// One record-change event (a single <c>st_log_change</c> row) in a record's history: the
    /// denormalised header (when / who / kind) plus the flattened before/after field changes it
    /// carried across the master and its detail rows.
    /// </summary>
    [MessagePackObject]
    public class RecordHistoryEntry
    {
        /// <summary>
        /// Gets or sets the log row's unique id (<c>st_log_change.sys_rowid</c>).
        /// </summary>
        [Key(0)]
        public Guid SysRowId { get; set; }

        /// <summary>
        /// Gets or sets the event time in UTC (<c>st_log_change.log_time</c>).
        /// </summary>
        [Key(1)]
        public DateTime LogTime { get; set; }

        /// <summary>
        /// Gets or sets the acting user's login id (denormalised).
        /// </summary>
        [Key(2)]
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the acting user's display name (denormalised).
        /// </summary>
        [Key(3)]
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the change kind derived from the master row state.
        /// </summary>
        [Key(4)]
        public ChangeKind ChangeKind { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this change touched sensitive data.
        /// </summary>
        [Key(5)]
        public bool IsSensitive { get; set; }

        /// <summary>
        /// Gets or sets the origin marker (e.g. <c>"Employee.Save"</c>).
        /// </summary>
        [Key(6)]
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the field-level before/after changes carried by this event, flattened across
        /// the master and its detail rows. Empty when the DiffGram carried no restorable changes.
        /// </summary>
        [Key(7)]
        public List<RecordFieldChange> Fields { get; set; } = [];

        // Add new fields starting from Key(8).
    }
}
