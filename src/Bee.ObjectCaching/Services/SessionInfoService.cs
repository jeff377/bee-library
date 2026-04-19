using Bee.Definition;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Session information access service.
    /// </summary>
    public class SessionInfoService : ISessionInfoService
    {
        /// <summary>
        /// Gets the session information from the cache, falling back to the database on a cache miss.
        /// </summary>
        public SessionInfo Get(Guid accessToken)
        {
            return CacheFunc.GetSessionInfo(accessToken)!;
        }

        /// <summary>
        /// Stores the session information in the cache, persisting it if necessary.
        /// </summary>
        public void Set(SessionInfo sessionInfo)
        {
            CacheFunc.SetSessionInfo(sessionInfo);
        }

        /// <summary>
        /// Removes the specified session information from the cache, invalidating any persisted state if necessary.
        /// </summary>
        public void Remove(Guid accessToken)
        {
            CacheFunc.RemoveSessionInfo(accessToken);   
        }
    }
}
