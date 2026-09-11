using System.Data;
using Bee.Definition.Logging;

namespace Bee.Api.Contracts.AuditLog
{
    /// <summary>
    /// Contract interface for the get-change-detail response: one change event's denormalised header
    /// plus its <c>changes_xml</c> payload restored into structured field-level before/after values and
    /// into a DataSet.
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

        /// <summary>
        /// Gets the change as a DataSet rebuilt from the stored payload, master and detail tables
        /// included: added rows for an insert, modified rows carrying their original values for an
        /// update, and the record as it stood before the delete for a delete. <c>null</c> when the
        /// payload cannot be rebuilt into a DataSet.
        /// </summary>
        /// <remarks>
        /// Read the rows by their row state. A delete comes back as unchanged rows holding the deleted
        /// record; deletes recorded by earlier versions, and a record deleted through a save, come back
        /// as deleted rows whose original values hold it instead. Events recorded before the payload
        /// carried its own schema, and deletes stored without their record, have no DataSet.
        /// </remarks>
        DataSet? DataSet { get; }
    }
}
