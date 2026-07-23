using System.Data;
using Bee.Api.Contracts.Form;
using Bee.Definition.Paging;

namespace Bee.Business.Form
{
    /// <summary>
    /// Output result for the FormSchema-driven GetList operation.
    /// </summary>
    public class GetListResult : BusinessResult, IGetListResponse
    {
        /// <summary>
        /// Gets or sets the result rows.
        /// </summary>
        public DataTable? Table { get; set; }

        /// <summary>
        /// Gets or sets the paging metadata; <c>null</c> when the query was unpaged.
        /// </summary>
        public PagingInfo? Paging { get; set; }
    }
}
