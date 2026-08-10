using Bee.Api.Contracts.Form;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API request for the form GetData operation.
    /// </summary>
    public class GetDataRequest : ApiRequest, IGetDataRequest
    {
        /// <summary>
        /// Gets or sets the master row identifier (<c>sys_rowid</c>) to load.
        /// </summary>
        public Guid RowId { get; set; }
    }
}
