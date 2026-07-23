using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form Delete operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class DeleteResponse : ApiResponse, IDeleteResponse
    {
        /// <summary>
        /// Gets or sets the number of master rows actually deleted.
        /// </summary>
        public int RowsAffected { get; set; }
    }
}
