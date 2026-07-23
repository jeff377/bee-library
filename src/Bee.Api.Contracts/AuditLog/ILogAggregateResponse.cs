using System.Data;

namespace Bee.Api.Contracts.AuditLog
{
    /// <summary>
    /// Shared contract for an audit-log aggregate response: a bounded, unpaged summary result set
    /// (dimension columns plus metric columns). Used by the monitoring summary / top-N queries; the
    /// <see cref="Table"/> carries whichever columns that aggregate projects.
    /// </summary>
    public interface ILogAggregateResponse
    {
        /// <summary>Gets the aggregate result rows (e.g. one row per <c>anomaly_kind</c> or per <c>method</c>).</summary>
        DataTable? Table { get; }
    }
}
