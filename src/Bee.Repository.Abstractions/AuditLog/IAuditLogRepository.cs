using System.Data;

namespace Bee.Repository.Abstractions.AuditLog
{
    /// <summary>
    /// Read-only data access over the <c>st_log_*</c> audit tables in the log database. The log
    /// database is physically separate and its rows are self-sufficient (denormalised who / company),
    /// so queries never join other databases.
    /// </summary>
    public interface IAuditLogRepository
    {
        /// <summary>
        /// Reads all <c>st_log_change</c> rows for one record (a <c>progId</c> plus its master
        /// <c>row_key</c>), ordered by <c>log_time</c> descending (newest first). Each row still
        /// carries its raw <c>changes_xml</c> DiffGram for the caller to restore.
        /// </summary>
        /// <param name="progId">The business object (program) id.</param>
        /// <param name="rowKey">The master record key (its <c>sys_rowid</c>).</param>
        /// <param name="companyId">
        /// When non-empty, restricts the result to rows whose denormalised <c>company_id</c> matches,
        /// so a caller can only read their own company's trail; when <c>null</c>/empty, no company
        /// restriction is applied.
        /// </param>
        /// <returns>The matching change rows as a <see cref="DataTable"/>; empty when none match.</returns>
        DataTable GetRecordChangeHistory(string progId, string rowKey, string? companyId);
    }
}
