namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the GetData request.
    /// </summary>
    /// <remarks>
    /// Step 1 of the "load + save" flow. Loads the master row identified by
    /// <see cref="RowId"/> together with its detail tables.
    /// </remarks>
    public interface IGetDataRequest
    {
        /// <summary>
        /// Gets the master row identifier (<c>sys_rowid</c>) to load.
        /// </summary>
        Guid RowId { get; }
    }
}
