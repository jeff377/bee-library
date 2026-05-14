using Bee.Definition.Filters;
using Bee.Definition.Paging;
using Bee.Definition.Sorting;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the GetList request.
    /// </summary>
    /// <remarks>
    /// The target table is always the master table of the schema identified by
    /// <c>ProgId</c> (the framework enforces <c>FormSchema.MasterTable.TableName == ProgId</c>),
    /// so no table name is carried on the request.
    /// </remarks>
    public interface IGetListRequest
    {
        /// <summary>
        /// Gets the comma-separated field names; an empty value falls back to
        /// <c>FormSchema.ListFields</c>, then to all fields.
        /// </summary>
        string SelectFields { get; }

        /// <summary>
        /// Gets the filter condition tree; <c>null</c> indicates an unfiltered query.
        /// </summary>
        FilterNode? Filter { get; }

        /// <summary>
        /// Gets the sort field collection; <c>null</c> uses the default ordering.
        /// </summary>
        SortFieldCollection? SortFields { get; }

        /// <summary>
        /// Gets the paging options; <c>null</c> means the query is unpaged and
        /// returns every matching row.
        /// </summary>
        PagingOptions? Paging { get; }
    }
}
