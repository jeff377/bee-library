using Bee.Api.Contracts.Form;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API request for the form Delete operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class DeleteRequest : ApiRequest, IDeleteRequest
    {
        /// <summary>
        /// Gets or sets the master row identifier (<c>sys_rowid</c>) to delete.
        /// </summary>
        public Guid RowId { get; set; }
    }
}
