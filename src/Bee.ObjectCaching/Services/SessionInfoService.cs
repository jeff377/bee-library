using Bee.Definition.Identity;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Session information access service. Ctor-injects <see cref="ICacheContainer"/>
    /// so per-host (or per-test-fixture) DI containers own their own session cache.
    /// </summary>
    public class SessionInfoService : ISessionInfoService
    {
        private readonly ICacheContainer _cache;

        /// <summary>
        /// Initializes a new <see cref="SessionInfoService"/> backed by the supplied cache container.
        /// </summary>
        /// <param name="cache">The cache container hosting the session cache.</param>
        public SessionInfoService(ICacheContainer cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Gets the session information from the cache, falling back to the database on a cache miss.
        /// </summary>
        public SessionInfo Get(Guid accessToken)
        {
            return _cache.SessionInfo.Get(accessToken)!;
        }

        /// <summary>
        /// Stores the session information in the cache, persisting it if necessary.
        /// </summary>
        public void Set(SessionInfo sessionInfo)
        {
            _cache.SessionInfo.Set(sessionInfo);
        }

        /// <summary>
        /// Removes the specified session information from the cache, invalidating any persisted state if necessary.
        /// </summary>
        public void Remove(Guid accessToken)
        {
            _cache.SessionInfo.Remove(accessToken);
        }
    }
}
