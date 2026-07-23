using Bee.Api.Contracts;
using Bee.Definition.Filters;
using Bee.Definition.Paging;
using Bee.Definition.Sorting;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API request for the form GetList operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetListRequest : ApiRequest, IGetListRequest
    {
        /// <summary>
        /// Gets or sets the comma-separated field names; an empty value falls back to
        /// <c>FormSchema.ListFields</c>, then to all fields.
        /// </summary>
        public string SelectFields { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the filter condition tree; <c>null</c> indicates an unfiltered query.
        /// </summary>
        public FilterNode? Filter { get; set; }

        /// <summary>
        /// Gets or sets the sort field collection; <c>null</c> uses the default ordering.
        /// </summary>
        public SortFieldCollection? SortFields { get; set; }

        /// <summary>
        /// Gets or sets the paging options; <c>null</c> means the query is unpaged and
        /// returns every matching row.
        /// </summary>
        public PagingOptions? Paging { get; set; }
    }
}
