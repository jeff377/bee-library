using Bee.Definition;

namespace Bee.Db.Dml
{
    /// <summary>
    /// Defines the interface for building a SQL ORDER BY clause.
    /// </summary>
    public interface ISortBuilder
    {
        /// <summary>
        /// Builds the SQL ORDER BY clause (including the keyword prefix) from the specified sort fields.
        /// </summary>
        /// <param name="sortFields">The collection of sort fields.</param>
        /// <param name="selectContext">The field source mappings and table JOIN relationships for the query.</param>
        string Build(SortFieldCollection? sortFields, SelectContext? selectContext = null);
    }
}
