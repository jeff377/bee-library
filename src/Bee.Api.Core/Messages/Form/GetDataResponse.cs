using System.Data;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form GetData operation.
    /// </summary>
    [MessagePackObject]
    public class GetDataResponse : ApiResponse, IGetDataResponse
    {
        /// <summary>
        /// Gets or sets the loaded <c>DataSet</c>; <c>null</c> when no row
        /// matches <c>RowId</c>.
        /// </summary>
        [Key(100)]
        public DataSet? DataSet { get; set; }
    }
}
