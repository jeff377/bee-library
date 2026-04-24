using Bee.Db.Schema;

namespace Bee.Db.Providers
{
    /// <summary>
    /// Provider-specific builder for the "rebuild" fallback upgrade path (tmp table + copy + swap).
    /// Used by the orchestrator when ALTER cannot apply all changes in place.
    /// </summary>
    public interface ITableRebuildCommandBuilder
    {
        /// <summary>
        /// Generates the full rebuild script for the given diff.
        /// </summary>
        /// <param name="diff">The schema diff; must not be a new-table diff.</param>
        string GetCommandText(TableSchemaDiff diff);
    }
}
