using Bee.Api.Contracts.Form;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form Delete operation.
    /// </summary>
    public class DeleteResponse : ApiResponse, IDeleteResponse
    {
        /// <summary>
        /// Gets or sets the number of master rows actually deleted.
        /// </summary>
        public int RowsAffected { get; set; }
    }
}
