using Bee.Definition.Filters;
using Bee.Definition;

namespace Bee.Db.Query
{
    /// <summary>
    /// Defines the interface for building a SQL WHERE clause.
    /// </summary>
    public interface IWhereBuilder
    {
        /// <summary>
        /// Builds the WHERE clause from a structured filter node tree.
        /// </summary>
        /// <param name="root">The root filter node (may be a group or a single condition).</param>
        /// <param name="selectContext">The field source mappings and table JOIN relationships for the query.</param>
        /// <param name="includeWhereKeyword">Whether to prepend the "WHERE " keyword to the result.</param>
        WhereBuildResult Build(FilterNode? root, SelectContext? selectContext = null, bool includeWhereKeyword = true);
    }
}
