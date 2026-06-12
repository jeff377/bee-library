using System.Data;
using Bee.Api.Contracts;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form GetLookup operation.
    /// </summary>
    [MessagePackObject]
    public class GetLookupResponse : ApiResponse, IGetLookupResponse
    {
        /// <summary>
        /// Gets or sets the lookup candidate rows.
        /// </summary>
        [Key(100)]
        public DataTable? Table { get; set; }

        /// <summary>
        /// Gets or sets the paging metadata; <c>null</c> when the query was unpaged.
        /// </summary>
        [Key(101)]
        public PagingInfo? Paging { get; set; }
    }
}
