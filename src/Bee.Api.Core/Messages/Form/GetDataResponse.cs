using System.Data;
using Bee.Api.Contracts.Form;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form GetData operation.
    /// </summary>
    public class GetDataResponse : ApiResponse, IGetDataResponse
    {
        /// <summary>
        /// Gets or sets the loaded <c>DataSet</c>; <c>null</c> when no row
        /// matches <c>RowId</c>.
        /// </summary>
        public DataSet? DataSet { get; set; }
    }
}
