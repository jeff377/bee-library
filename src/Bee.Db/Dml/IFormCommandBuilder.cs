using Bee.Definition.Filters;
using Bee.Definition.Sorting;

namespace Bee.Db.Dml
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
        /// <param name="skip">Rows to skip; null means no offset.</param>
        /// <param name="take">Rows to take; null means no row limit.</param>
        DbCommandSpec BuildSelect(string tableName, string selectFields, FilterNode? filter = null, SortFieldCollection? sortFields = null,
            int? skip = null, int? take = null);

        /// <summary>
        /// Builds the SELECT COUNT(*) command specification for the specified table using the supplied filter.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="filter">The filter condition.</param>
        DbCommandSpec BuildCount(string tableName, FilterNode? filter = null);

        /// <summary>
        /// Builds the DELETE command specification for the specified table using the supplied filter.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="filter">The filter that becomes the WHERE clause; must not be null.</param>
        DbCommandSpec BuildDelete(string tableName, FilterNode filter);
    }
}
