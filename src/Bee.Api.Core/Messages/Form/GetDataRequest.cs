using Bee.Api.Contracts.Form;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API request for the form GetData operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetDataRequest : ApiRequest, IGetDataRequest
    {
        /// <summary>
        /// Gets or sets the master row identifier (<c>sys_rowid</c>) to load.
        /// </summary>
        public Guid RowId { get; set; }
    }
}
