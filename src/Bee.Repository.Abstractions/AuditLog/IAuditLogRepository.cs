using System.Data;
using Bee.Definition.Paging;

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
        /// Reads a page of <c>st_log_change</c> event headers matching <paramref name="query"/>, ordered
        /// by <c>log_time</c> descending (newest first, with <c>sys_no</c> as a deterministic tiebreak).
        /// Header columns only — the <c>changes_xml</c> DiffGram is intentionally excluded; fetch a
        /// single event's detail with <see cref="GetChangeById"/> when needed.
        /// </summary>
        /// <param name="query">The typed, AND-combined filter (all fields optional).</param>
        /// <param name="paging">The page request (page / size / whether to include the total count).</param>
        /// <returns>The header rows plus paging metadata.</returns>
        AuditLogPage GetChangeLog(ChangeLogQuery query, PagingOptions paging);

        /// <summary>
        /// Reads a single <c>st_log_change</c> row by its <c>sys_rowid</c>, including the raw
        /// <c>changes_xml</c> DiffGram for the caller to restore.
        /// </summary>
        /// <param name="sysRowId">The log row's unique id (<c>st_log_change.sys_rowid</c>).</param>
        /// <param name="companyId">
        /// When non-empty, additionally requires the row's denormalised <c>company_id</c> to match, so a
        /// caller cannot read another company's event by id.
        /// </param>
        /// <returns>A one-row <see cref="DataTable"/>, or <c>null</c> when no such row is in scope.</returns>
        DataTable? GetChangeById(Guid sysRowId, string? companyId);

        /// <summary>
        /// Reads a page of <c>st_log_login</c> event headers matching <paramref name="query"/>, newest first.
        /// </summary>
        /// <param name="query">The typed, AND-combined filter (all fields optional).</param>
        /// <param name="paging">The page request.</param>
        AuditLogPage GetLoginLog(LoginLogQuery query, PagingOptions paging);

        /// <summary>
        /// Reads a page of <c>st_log_access</c> record-view headers matching <paramref name="query"/>, newest first.
        /// </summary>
        /// <param name="query">The typed, AND-combined filter (all fields optional).</param>
        /// <param name="paging">The page request.</param>
        AuditLogPage GetAccessLog(AccessLogQuery query, PagingOptions paging);

        /// <summary>
        /// Reads a page of <c>st_log_anomaly_api</c> API-anomaly headers matching <paramref name="query"/>, newest first.
        /// </summary>
        /// <param name="query">The typed, AND-combined filter (all fields optional).</param>
        /// <param name="paging">The page request.</param>
        AuditLogPage GetApiAnomalyLog(ApiAnomalyLogQuery query, PagingOptions paging);

        /// <summary>
        /// Reads a page of <c>st_log_anomaly_db</c> DB-anomaly headers matching <paramref name="query"/>, newest first.
        /// A cross-company infrastructure view (the table carries no company).
        /// </summary>
        /// <param name="query">The typed, AND-combined filter (all fields optional).</param>
        /// <param name="paging">The page request.</param>
        AuditLogPage GetDbAnomalyLog(DbAnomalyLogQuery query, PagingOptions paging);

        /// <summary>
        /// Aggregates <c>st_log_anomaly_api</c> counts by <c>anomaly_kind</c> over an optional time window,
        /// scoped to <paramref name="companyId"/> when non-empty. Returns <c>anomaly_kind</c> / <c>event_count</c>.
        /// </summary>
        DataTable GetApiAnomalySummary(DateTime? fromUtc, DateTime? toUtc, string? companyId);

        /// <summary>
        /// Aggregates <c>st_log_anomaly_db</c> counts by <c>anomaly_kind</c> over an optional time window.
        /// A cross-company infrastructure summary (the table carries no company). Returns
        /// <c>anomaly_kind</c> / <c>event_count</c>.
        /// </summary>
        DataTable GetDbAnomalySummary(DateTime? fromUtc, DateTime? toUtc);

        /// <summary>
        /// Returns the top-<paramref name="topN"/> API methods by <c>st_log_anomaly_api</c> count over an
        /// optional time window, scoped to <paramref name="companyId"/> when non-empty. Returns
        /// <c>method</c> / <c>event_count</c> / <c>max_elapsed_ms</c>, busiest first.
        /// </summary>
        DataTable GetTopApiMethods(DateTime? fromUtc, DateTime? toUtc, int topN, string? companyId);
    }
}
