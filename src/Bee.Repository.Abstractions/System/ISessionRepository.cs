using Bee.Definition.Identity;

namespace Bee.Repository.Abstractions.System
{
    /// <summary>
    /// Data access interface for the session seed stored in <c>st_session</c>.
    /// </summary>
    /// <remarks>
    /// The row is a seed, not a snapshot of the session: it holds only what cannot be derived
    /// again (token, user, expiry, company). Everything else is recomputed when the session is
    /// rebuilt, so a permission revoked after sign-in does not survive in a stale copy.
    /// </remarks>
    public interface ISessionRepository
    {
        /// <summary>
        /// Gets the session seed for the specified access token, or <c>null</c> when no live
        /// session exists for it.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        SessionUser? GetSession(Guid accessToken);

        /// <summary>
        /// Writes the seed for a newly issued session.
        /// </summary>
        /// <param name="sessionUser">The seed to persist.</param>
        /// <remarks>
        /// Called before the session enters the cache. The order matters: a row with no cache
        /// entry is rebuilt on the next request, whereas a cache entry with no row is a session
        /// that dies at the next restart and does not exist at all on another node.
        /// </remarks>
        void InsertSession(SessionUser sessionUser);

        /// <summary>
        /// Overwrites the seed of an existing session, matched by
        /// <see cref="SessionUser.AccessToken"/>. A no-op when the row is gone.
        /// </summary>
        /// <param name="sessionUser">The seed to write.</param>
        void UpdateSession(SessionUser sessionUser);

        /// <summary>
        /// Deletes the seed for the specified access token. Idempotent.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <remarks>
        /// Required for logout to mean anything once sessions are rebuilt from this table:
        /// clearing only the cache would let the next request restore the token from the row.
        /// </remarks>
        void DeleteSession(Guid accessToken);
    }
}
