using System.Data;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API request for the form Save operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class SaveRequest : ApiRequest, ISaveRequest
    {
        /// <summary>
        /// Gets or sets the <c>DataSet</c> to persist. Each row's
        /// <c>RowState</c> dispatches to INSERT / UPDATE / DELETE on the
        /// server.
        /// </summary>
        public DataSet? DataSet { get; set; }
    }
}
