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

        /// <summary>
        /// Reads a user's localization preferences (<c>st_user.time_zone</c> and
        /// <c>st_user.culture</c>) in one query, returning <see cref="UserLocale.Empty"/> when the
        /// user does not exist.
        /// </summary>
        /// <param name="userId">The user business id (<c>st_user.sys_id</c>).</param>
        /// <remarks>
        /// The caller decides the fallback for each value. An empty entry is expected rather than
        /// exceptional: rows seeded before the columns existed carry no value, and a deployment may
        /// leave them unset entirely. See docs/adr/adr-032-datetime-timezone.md (D12) for why the
        /// user's zone — not the device's and not the server's — is the authority for user-facing
        /// dates; the culture is stored per user for the same reason plus one more, namely that a
        /// background service sending a notification has no session to read it from.
        /// </remarks>
        UserLocale GetLocale(string userId);
    }
}
