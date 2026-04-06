using System;

namespace Bee.Define
{
    /// <summary>
    /// Interface for a session info access service.
    /// Uses cache as the primary source, with fallback to database loading or persistence when necessary.
    /// </summary>
    public interface ISessionInfoService
    {
        /// <summary>
        /// Gets session info from cache (with fallback to database on a cache miss).
        /// </summary>
        SessionInfo Get(Guid accessToken);

        /// <summary>
        /// Stores session info in cache (and persists if necessary).
        /// </summary>
        void Set(SessionInfo sessionInfo);

        /// <summary>
        /// Removes the specified session info from cache (and invalidates the persisted state if necessary).
        /// </summary>
        void Remove(Guid accessToken);
    }
}
