using Bee.Definition.Logging;
using MessagePack;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// One field's before/after values within a single changed row of a record-change event.
    /// Restored server-side from the <c>st_log_change.changes_xml</c> DataSet DiffGram.
    /// </summary>
    /// <remarks>
    /// For an insert, <see cref="OldValue"/> is <c>null</c> and <see cref="NewValue"/> carries the
    /// inserted value. For a delete, <see cref="NewValue"/> is <c>null</c> and <see cref="OldValue"/>
    /// carries the before-image. For an update, only fields whose value actually changed are emitted,
    /// each carrying both the old and the new value.
    /// </remarks>
    [MessagePackObject]
    public class RecordFieldChange
    {
        /// <summary>
        /// Gets or sets the table the changed row belongs to (master or a detail table).
        /// </summary>
        [Key(0)]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the changed row's own key (its <c>sys_rowid</c>), when present in the DiffGram.
        /// </summary>
        [Key(1)]
        public string? RowKey { get; set; }

        /// <summary>
        /// Gets or sets the row's change kind (insert / update / delete of that row).
        /// </summary>
        [Key(2)]
        public ChangeKind RowState { get; set; }

        /// <summary>
        /// Gets or sets the changed field's name.
        /// </summary>
        [Key(3)]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value before the change; <c>null</c> for an inserted row.
        /// </summary>
        [Key(4)]
        public string? OldValue { get; set; }

        /// <summary>
        /// Gets or sets the value after the change; <c>null</c> for a deleted row.
        /// </summary>
        [Key(5)]
        public string? NewValue { get; set; }

        // Add new fields starting from Key(6).
    }
}
