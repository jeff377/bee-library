using Bee.Definition.Identity;

namespace Bee.ObjectCaching.Database
{
    /// <summary>
    /// Session information cache.
    /// </summary>
    public class SessionInfoCache : KeyObjectCache<SessionInfo>
    {
        /// <summary>
        /// Initializes a new <see cref="SessionInfoCache"/>.
        /// </summary>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public SessionInfoCache(string cachePrefix = "") : base(cachePrefix) { }

        /// <summary>
        /// Creates an instance of the session information.
        /// </summary>
        /// <param name="key">The access token.</param>
        protected override SessionInfo? CreateInstance(string key)
        {
            return null; // Loading SessionInfo from the database or other sources is not yet implemented
        }

        /// <summary>
        /// Disables negative caching for session lookups.
        /// </summary>
        /// <remarks>
        /// Session entries are populated exclusively by Login via
        /// <see cref="KeyObjectCache{T}.Set(T)"/> and never rebuilt from a backing store
        /// (<see cref="CreateInstance"/> always returns <c>null</c>). Caching every
        /// unauthenticated lookup as a negative entry would let anonymous traffic
        /// inflate the cache with markers for arbitrary access tokens without
        /// preventing meaningful work, since the lookup already returns <c>null</c>
        /// fast for unknown tokens.
        /// </remarks>
        /// <param name="key">The access token (unused).</param>
        protected override CacheItemPolicy? GetNegativePolicy(string key) => null;

        /// <summary>
        /// Gets the session information for the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public SessionInfo? Get(Guid accessToken)
        {
            return Get(accessToken.ToString());
        }

        /// <summary>
        /// Removes the session information from the cache.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public void Remove(Guid accessToken)
        {
            Remove(accessToken.ToString());
        }
    }
}
