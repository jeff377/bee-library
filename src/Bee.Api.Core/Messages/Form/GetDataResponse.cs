using System.Data;
using Bee.Api.Contracts.Form;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form GetData operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetDataResponse : ApiResponse, IGetDataResponse
    {
        /// <summary>
        /// Gets or sets the loaded <c>DataSet</c>; <c>null</c> when no row
        /// matches <c>RowId</c>.
        /// </summary>
        public DataSet? DataSet { get; set; }
    }
}
