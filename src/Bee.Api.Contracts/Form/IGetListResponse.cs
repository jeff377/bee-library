using System.Data;
using Bee.Definition.Paging;

namespace Bee.Api.Contracts.Form
{
    /// <summary>
    /// Contract interface for the GetList response.
    /// </summary>
    public interface IGetListResponse
    {
        /// <summary>
        /// Gets the result rows.
        /// </summary>
        DataTable? Table { get; }

        /// <summary>
        /// Gets the paging metadata; <c>null</c> when the query was unpaged.
        /// </summary>
        PagingInfo? Paging { get; }
    }
}
