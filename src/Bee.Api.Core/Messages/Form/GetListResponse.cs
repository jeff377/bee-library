using System.Data;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form GetList operation.
    /// </summary>
    /// <remarks>
    /// <c>Key(101)</c> is reserved for a future <c>PagingInfo</c> field; see
    /// <c>docs/plans/plan-formbo-getlist.md</c> (Range out / paging section).
    /// </remarks>
    [MessagePackObject]
    public class GetListResponse : ApiResponse, IGetListResponse
    {
        /// <summary>
        /// Gets or sets the result rows.
        /// </summary>
        [Key(100)]
        public DataTable? Table { get; set; }
    }
}
