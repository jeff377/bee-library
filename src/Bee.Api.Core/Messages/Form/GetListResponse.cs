using System.Data;
using Bee.Api.Contracts.Form;
using Bee.Definition.Paging;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form GetList operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetListResponse : ApiResponse, IGetListResponse
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
