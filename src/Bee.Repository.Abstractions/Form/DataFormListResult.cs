using System.Data;
using Bee.Definition.Paging;

namespace Bee.Repository.Abstractions.Form
{
    /// <summary>
    /// Result of a list-style query. Carries the row data and optional paging
    /// metadata when the underlying query was paged.
    /// </summary>
    public sealed class DataFormListResult
    {
        /// <summary>
        /// Gets the result rows; <c>null</c> when the underlying database command
        /// produced no table.
        /// </summary>
        public DataTable? Table { get; init; }

        /// <summary>
        /// Gets the paging metadata; <c>null</c> when the query was not paged.
        /// </summary>
        public PagingInfo? Paging { get; init; }
    }
}
