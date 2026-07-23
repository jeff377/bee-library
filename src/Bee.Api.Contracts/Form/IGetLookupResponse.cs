using System.Data;
using Bee.Definition.Paging;

namespace Bee.Api.Contracts.Form
{
    /// <summary>
    /// Contract interface for the GetLookup response.
    /// </summary>
    public interface IGetLookupResponse
    {
        /// <summary>
        /// Gets the lookup candidate rows.
        /// </summary>
        DataTable? Table { get; }

        /// <summary>
        /// Gets the paging metadata; <c>null</c> when the query was unpaged.
        /// </summary>
        PagingInfo? Paging { get; }
    }
}
