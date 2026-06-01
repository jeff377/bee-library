namespace Bee.ObjectCaching
{
    /// <summary>
    /// Non-generic eviction surface implemented by every framework cache so the cache container
    /// can dispatch a <c>"group:entity"</c> invalidation to the right cache by convention, without
    /// a hand-maintained route table and without the caching layer depending on any data source.
    /// </summary>
    /// <remarks>
    /// The cache-notify poller reads a bumped cache key from the notification table and hands it to
    /// <see cref="ICacheContainer.TryEvict(string)"/>; the container looks the key's group up among
    /// the caches it owns and calls <see cref="Evict(string)"/>. A new DB-dependent cache becomes
    /// invalidatable simply by being added to the container — no per-cache registration.
    /// </remarks>
    public interface IEvictableCache
    {
        /// <summary>
        /// Gets the cache group this cache answers to — the prefix of a <c>"group:entity"</c> cache
        /// key (e.g. <c>"CompanyInfo"</c>). Defaults to the cached type's name.
        /// </summary>
        string CacheGroup { get; }

        /// <summary>
        /// Evicts the entry identified by <paramref name="entity"/> (the part of the cache key after
        /// the first <c>:</c>). Keyed caches remove that single entry; single-object caches remove
        /// their one entry regardless of <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity portion of the cache key.</param>
        void Evict(string entity);
    }
}
