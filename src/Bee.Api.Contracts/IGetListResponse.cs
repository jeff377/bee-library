using System.Data;

namespace Bee.Api.Contracts
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
    }
}
