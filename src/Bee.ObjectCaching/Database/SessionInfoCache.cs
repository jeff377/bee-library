using Bee.Definition;
using Bee.Definition.Identity;

namespace Bee.ObjectCaching.Database
{
    /// <summary>
    /// Session information cache.
    /// </summary>
    public class SessionInfoCache : KeyObjectCache<SessionInfo>
    {
        private readonly Func<ICacheDataSourceProvider>? _dataSource;

        /// <summary>
        /// Initializes a new <see cref="SessionInfoCache"/> with no data source, so every miss
        /// stays a miss.
        /// </summary>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public SessionInfoCache(string cachePrefix = "") : this(null, cachePrefix) { }

        /// <summary>
        /// Initializes a new <see cref="SessionInfoCache"/> that rebuilds sessions through the
        /// supplied data source.
        /// </summary>
        /// <param name="dataSource">
        /// Factory resolving the data source on the first miss. WARNING: it must stay a factory —
        /// resolving the provider while this container is still under construction closes a
        /// dependency cycle back onto the container.
        /// </param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        internal SessionInfoCache(Func<ICacheDataSourceProvider>? dataSource, string cachePrefix)
            : base(cachePrefix)
        {
            _dataSource = dataSource;
        }

        /// <summary>
        /// Rebuilds the session from its persisted seed on a cache miss.
        /// </summary>
        /// <param name="key">The access token.</param>
        /// <remarks>
        /// WARNING: this makes every writer of <c>st_session</c> a way to mint a token that
        /// satisfies <c>AccessTokenValidator</c> — before this existed, only sign-in could put a
        /// session in the cache, and rows in the table were inert. Any new writer must therefore
        /// authenticate for itself or be confined to trusted callers, which is why
        /// <c>SystemBO.CreateSession</c> (a token from a user id alone) is <c>LocalOnly</c>.
        ///
        /// The rebuild returns <c>null</c> for anything that is not a live session — no seed, an
        /// expired seed, revoked company access, or a key provider that cannot recover the session
        /// key — so an unknown token still fails to authenticate.
        /// </remarks>
        protected override SessionInfo? CreateInstance(string key)
        {
            return Guid.TryParse(key, out var accessToken)
                ? _dataSource?.Invoke().GetSessionInfo(accessToken)
                : null;
        }

        /// <summary>
        /// Disables negative caching for session lookups.
        /// </summary>
        /// <remarks>
        /// Caching every unauthenticated lookup as a negative entry would let anonymous traffic
        /// inflate the cache with markers for arbitrary access tokens. The rebuild it would be
        /// saving is a single indexed read on <c>access_token</c> that returns nothing, so the
        /// marker buys little and costs memory an attacker chooses the size of.
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
