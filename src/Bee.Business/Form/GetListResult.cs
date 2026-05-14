using System.Data;
using Bee.Api.Contracts;

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
    }
}
