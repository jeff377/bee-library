namespace Bee.Api.Contracts.Form
{
    /// <summary>
    /// Contract interface for the Delete request.
    /// </summary>
    /// <remarks>
    /// Direct single-row delete. UI delete-button flows use this to avoid the
    /// round-trip cost of loading the full <c>DataSet</c> just to mark a row
    /// as <c>Deleted</c> before calling <c>Save</c>.
    /// </remarks>
    public interface IDeleteRequest
    {
        /// <summary>
        /// Gets the master row identifier (<c>sys_rowid</c>) to delete.
        /// </summary>
        Guid RowId { get; }
    }
}
