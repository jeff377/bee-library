using Bee.Definition.Filters;
using Bee.Definition.Paging;
using Bee.Definition.Sorting;

namespace Bee.Repository.Abstractions.Form
{
    /// <summary>
    /// Repository interface for data forms.
    /// </summary>
    public interface IDataFormRepository
    {
        /// <summary>
        /// Retrieves list-view rows from the master table by executing a
        /// FormSchema-driven SELECT statement.
        /// </summary>
        /// <param name="selectFields">
        /// The comma-separated field names to retrieve; an empty value falls back to
        /// <c>FormSchema.ListFields</c>, then to all fields.
        /// </param>
        /// <param name="filter">The filter condition tree; <c>null</c> for an unfiltered query.</param>
        /// <param name="sortFields">The sort field collection; <c>null</c> uses the default ordering.</param>
        /// <param name="paging">The paging options; <c>null</c> returns every matching row.</param>
        /// <returns>
        /// A <see cref="DataFormListResult"/> with the row data and, when paging was
        /// requested, the corresponding <see cref="PagingInfo"/>.
        /// </returns>
        DataFormListResult GetList(
            string selectFields,
            FilterNode? filter,
            SortFieldCollection? sortFields,
            PagingOptions? paging = null);
    }
}
