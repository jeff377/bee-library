using System.Data;
using Bee.Api.Contracts.Form;
using Bee.Definition.Paging;

namespace Bee.Business.Form
{
    /// <summary>
    /// Output result for the form GetLookup operation.
    /// </summary>
    public class GetLookupResult : BusinessResult, IGetLookupResponse
    {
        /// <summary>
        /// Gets or sets the lookup candidate rows.
        /// </summary>
        public DataTable? Table { get; set; }

        /// <summary>
        /// Gets or sets the paging metadata; <c>null</c> when the query was unpaged.
        /// </summary>
        public PagingInfo? Paging { get; set; }
    }
}
