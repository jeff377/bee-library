using System.Data;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form GetNewData operation.
    /// </summary>
    [MessagePackObject]
    public class GetNewDataResponse : ApiResponse, IGetNewDataResponse
    {
        /// <summary>
        /// Gets or sets the blank <c>DataSet</c> skeleton; the master table
        /// carries one <c>Added</c> row seeded with FormSchema defaults and a
        /// server-issued <c>sys_rowid</c>.
        /// </summary>
        [Key(100)]
        public DataSet? DataSet { get; set; }
    }
}
