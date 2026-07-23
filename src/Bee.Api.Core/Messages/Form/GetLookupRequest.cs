using Bee.Api.Contracts;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API request for the form GetLookup operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetLookupRequest : ApiRequest, IGetLookupRequest
    {
        /// <summary>
        /// Gets or sets the search text matched against the string-typed lookup fields;
        /// an empty value applies no search filter.
        /// </summary>
        public string SearchText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the paging options; <c>null</c> applies the server-side default
        /// page size.
        /// </summary>
        public PagingOptions? Paging { get; set; }
    }
}
