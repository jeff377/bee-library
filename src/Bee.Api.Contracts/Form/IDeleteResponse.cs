namespace Bee.Api.Contracts.Form
{
    /// <summary>
    /// Contract interface for the Delete response.
    /// </summary>
    public interface IDeleteResponse
    {
        /// <summary>
        /// Gets the number of master rows actually deleted (zero indicates the
        /// row no longer exists; callers decide whether to treat that as an
        /// error).
        /// </summary>
        int RowsAffected { get; }
    }
}
