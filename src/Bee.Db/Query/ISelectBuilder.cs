using Bee.Definition.Forms;
using Bee.Definition;

namespace Bee.Db.Query
{
    /// <summary>
    /// Defines the interface for building a SQL SELECT clause.
    /// </summary>
    public interface ISelectBuilder
    {
        /// <summary>
        /// Builds the SELECT clause.
        /// </summary>
        /// <param name="formTable">The form table.</param>
        /// <param name="selectFields">A comma-separated string of field names to retrieve; an empty string retrieves all fields.</param>
        /// <param name="selectContext">The field source mappings and table JOIN relationships for the query.</param>
        string Build(FormTable formTable, string selectFields, SelectContext selectContext);
    }
}
