using Bee.Definition;

namespace Bee.Repository.Abstractions.System
{
    /// <summary>
    /// Data access interface for session information.
    /// </summary>
    public interface ISessionRepository
    {
        /// <summary>
        /// Gets the session information for the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        SessionUser? GetSession(Guid accessToken);

        /// <summary>
        /// Creates a new user session.
        /// </summary>
        /// <param name="userID">The user account identifier.</param>
        /// <param name="expiresIn">The expiration time in seconds.</param>
        /// <param name="oneTime">Whether the session is valid for one-time use only.</param>
        SessionUser CreateSession(string userID, int expiresIn = 3600, bool oneTime = false);
    }
}