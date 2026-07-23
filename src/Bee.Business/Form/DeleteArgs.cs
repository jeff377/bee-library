using Bee.Api.Contracts.Form;

namespace Bee.Business.Form
{
    /// <summary>
    /// Input arguments for the FormSchema-driven <c>Delete</c> operation.
    /// </summary>
    public class DeleteArgs : BusinessArgs, IDeleteRequest
    {
        /// <summary>
        /// Gets or sets the master row identifier (<c>sys_rowid</c>) to delete.
        /// </summary>
        public Guid RowId { get; set; }
    }
}
