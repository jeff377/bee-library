using System.Data;
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

        /// <summary>
        /// Produces a blank <c>DataSet</c> skeleton seeded with FormSchema
        /// defaults. The master table carries exactly one row in the
        /// <see cref="DataRowState.Added"/> state with a server-issued
        /// <c>sys_rowid</c>; detail tables carry their full schema but no rows.
        /// </summary>
        DataSet GetNewData();

        /// <summary>
        /// Loads the master row (and its details) by <paramref name="rowId"/>.
        /// All returned rows have <c>AcceptChanges</c> applied so their state
        /// is <see cref="DataRowState.Unchanged"/>.
        /// </summary>
        /// <param name="rowId">The master row identifier (<c>sys_rowid</c>).</param>
        /// <returns>
        /// The loaded <see cref="DataSet"/>; <c>null</c> when no master row
        /// matches <paramref name="rowId"/>.
        /// </returns>
        DataSet? GetData(Guid rowId);

        /// <summary>
        /// Persists changes from a <see cref="DataSet"/> by dispatching
        /// INSERT / UPDATE / DELETE based on each row's <see cref="DataRow.RowState"/>;
        /// every command runs inside a single transaction.
        /// </summary>
        /// <param name="dataSet">The DataSet to persist.</param>
        /// <returns>
        /// A tuple containing the freshly re-loaded <c>DataSet</c> and the
        /// per-table affected-row counts.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="dataSet"/> contains no pending changes
        /// (callers should not invoke <c>Save</c> in this case).
        /// </exception>
        (DataSet? Refreshed, Dictionary<string, int> AffectedRows) Save(DataSet dataSet);

        /// <summary>
        /// Deletes a single master row directly by <paramref name="rowId"/>.
        /// Detail rows that reference the master through
        /// <c>sys_master_rowid</c> are removed first; the entire operation
        /// runs inside a single transaction.
        /// </summary>
        /// <param name="rowId">The master row identifier (<c>sys_rowid</c>).</param>
        /// <returns>
        /// The number of master rows actually deleted (zero indicates the row
        /// no longer exists).
        /// </returns>
        int Delete(Guid rowId);
    }
}
