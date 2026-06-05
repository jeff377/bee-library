namespace Bee.Repository.Abstractions.System
{
    /// <summary>
    /// Data access for the common <c>st_user</c> table. Lives in the common database, so methods take
    /// no database id (the common category is resolved internally).
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Resolves a user's row id (<c>sys_rowid</c>) from its business id (<c>sys_id</c>).
        /// Returns <c>Guid.Empty</c> when no such user exists.
        /// </summary>
        /// <param name="userId">The user business id (<c>st_user.sys_id</c>).</param>
        Guid GetRowIdBySysId(string userId);
    }
}
