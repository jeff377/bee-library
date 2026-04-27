using Bee.Definition.Filters;
using Bee.Definition;

namespace Bee.Db.Providers
{
    /// <summary>
    /// Defines a form-related SQL command builder that generates Select, Insert, Update, and Delete statements.
    /// </summary>
    public interface IFormCommandBuilder
    {
        /// <summary>
        /// Builds the SELECT command specification.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="selectFields">A comma-separated list of field names to retrieve; empty string retrieves all fields.</param>
        /// <param name="filter">The filter condition.</param>
        /// <param name="sortFields">The sort field collection.</param>
        DbCommandSpec BuildSelect(string tableName, string selectFields, FilterNode? filter = null, SortFieldCollection? sortFields = null);

        /// <summary>
        /// Builds the INSERT command specification.
        /// </summary>
        DbCommandSpec BuildInsert();

        /// <summary>
        /// Builds the UPDATE command specification.
        /// </summary>
        DbCommandSpec BuildUpdate();

        /// <summary>
        /// Builds the DELETE command specification.
        /// </summary>
        DbCommandSpec BuildDelete();
    }
}
