using System.Data;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the Save response.
    /// </summary>
    public interface ISaveResponse
    {
        /// <summary>
        /// Gets the freshly re-loaded <c>DataSet</c>. Server-generated columns
        /// (timestamps, version numbers, computed fields) are merged in,
        /// deleted rows no longer appear, and every remaining row has
        /// <c>RowState == Unchanged</c>. Callers should replace their local
        /// copy with this value.
        /// </summary>
        DataSet? DataSet { get; }

        /// <summary>
        /// Gets the per-table affected-row counts (table name → rows touched),
        /// suitable for caller-side logging or UI status messages.
        /// </summary>
        Dictionary<string, int> AffectedRows { get; }
    }
}
