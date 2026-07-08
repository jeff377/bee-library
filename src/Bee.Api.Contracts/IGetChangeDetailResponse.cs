using Bee.Definition.Logging;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the get-change-detail response: one change event's denormalised header
    /// plus its <c>changes_xml</c> DiffGram restored into structured field-level before/after values.
    /// </summary>
    public interface IGetChangeDetailResponse
    {
        /// <summary>Gets the log row's unique id (<c>st_log_change.sys_rowid</c>).</summary>
        Guid SysRowId { get; }

        /// <summary>Gets the event time in UTC.</summary>
        DateTime LogTime { get; }

        /// <summary>Gets the acting user's login id (denormalised).</summary>
        string? UserId { get; }

        /// <summary>Gets the acting user's display name (denormalised).</summary>
        string? UserName { get; }

        /// <summary>Gets the business object (program) id.</summary>
        string? ProgId { get; }

        /// <summary>Gets the master record key (its <c>sys_rowid</c>).</summary>
        string? RowKey { get; }

        /// <summary>Gets the change kind derived from the master row state.</summary>
        ChangeKind ChangeKind { get; }

        /// <summary>Gets a value indicating whether this change touched sensitive data.</summary>
        bool IsSensitive { get; }

        /// <summary>Gets the origin marker (e.g. <c>"Employee.Save"</c>).</summary>
        string? Source { get; }

        /// <summary>
        /// Gets the field-level before/after changes carried by this event, flattened across the master
        /// and its detail rows. Empty when the DiffGram carried no restorable changes.
        /// </summary>
        IReadOnlyList<RecordFieldChange> Fields { get; }
    }
}
