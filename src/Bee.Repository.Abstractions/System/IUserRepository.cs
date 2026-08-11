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
        /// Verifies a password against the hash stored in <c>st_user.password</c>.
        /// </summary>
        /// <param name="userId">The user business id (<c>st_user.sys_id</c>).</param>
        /// <param name="password">The plain-text password supplied by the caller.</param>
        /// <returns><c>true</c> when the user exists and the password matches; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// WARNING: the stored hash never leaves this method. Returning it so the caller can compare
        /// would put a credential into a caller-side variable, which is one more place it can reach a
        /// log or an exception message.
        /// <para>
        /// An unknown user and a wrong password both return <c>false</c>, and deliberately so: a
        /// caller that could tell them apart would be an account enumeration oracle.
        /// </para>
        /// </remarks>
        bool VerifyPassword(string userId, string password);

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

        /// <summary>
        /// Reads a user's display name (<c>st_user.sys_name</c>), or <c>null</c> when no such user
        /// exists.
        /// </summary>
        /// <param name="userId">The user business id (<c>st_user.sys_id</c>).</param>
        /// <remarks>
        /// <c>null</c> means "no such user" and is distinct from an empty name, which is why the
        /// return type is nullable: the caller uses it to decide whether a session may be issued
        /// at all.
        /// </remarks>
        string? GetName(string userId);

        /// <summary>
        /// Reads a user's deployment administrator flag (<c>st_user.deployment_admin</c>).
        /// Returns <c>false</c> when no such user exists.
        /// </summary>
        /// <param name="userId">The user business id (<c>st_user.sys_id</c>).</param>
        /// <remarks>
        /// An unknown user collapsing into <c>false</c> is deliberate: every caller is asking an
        /// authorization question, and "no such user" and "not an administrator" both deny.
        /// </remarks>
        bool IsDeploymentAdmin(string userId);

        /// <summary>
        /// Grants or revokes a user's deployment administrator flag, returning <c>false</c> when no
        /// such user exists.
        /// </summary>
        /// <param name="userId">The user business id (<c>st_user.sys_id</c>).</param>
        /// <param name="isDeploymentAdmin">The flag value to store.</param>
        /// <remarks>
        /// WARNING: this is a privilege escalation path and must never be reachable from an ordinary
        /// user-maintenance form. The column is not part of any shipped FormSchema, and the only
        /// framework caller is a local-only business object method.
        /// </remarks>
        bool SetDeploymentAdmin(string userId, bool isDeploymentAdmin);
    }
}
