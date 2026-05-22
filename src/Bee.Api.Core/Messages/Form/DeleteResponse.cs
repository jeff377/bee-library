using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form Delete operation.
    /// </summary>
    [MessagePackObject]
    public class DeleteResponse : ApiResponse, IDeleteResponse
    {
        /// <summary>
        /// Gets or sets the number of master rows actually deleted.
        /// </summary>
        [Key(100)]
        public int RowsAffected { get; set; }
    }
}
