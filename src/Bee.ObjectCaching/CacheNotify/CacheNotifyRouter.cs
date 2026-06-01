using System.Collections.Concurrent;

namespace Bee.ObjectCaching.CacheNotify
{
    /// <summary>
    /// Default <see cref="ICacheNotifyRouter"/> implementation backed by a thread-safe map from
    /// cache group to eviction action. Routes are typically registered once at startup and read
    /// concurrently by the poller thread.
    /// </summary>
    public sealed class CacheNotifyRouter : ICacheNotifyRouter
    {
        // Group comparison is ordinal and case-insensitive: cache groups are well-known literals
        // (e.g. "OrgInfo", "FormSchema") and callers should not depend on their casing.
        private readonly ConcurrentDictionary<string, Action<ICacheContainer, string>> _routes =
            new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public void Register(string cacheGroup, Action<ICacheContainer, string> evict)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cacheGroup);
            ArgumentNullException.ThrowIfNull(evict);
            _routes[cacheGroup] = evict;
        }

        /// <inheritdoc/>
        public bool TryInvoke(ICacheContainer container, string cacheKey)
        {
            ArgumentNullException.ThrowIfNull(container);
            if (string.IsNullOrEmpty(cacheKey)) return false;

            // Split on the first ':' into group + entity; the entity may itself contain ':'.
            int separator = cacheKey.IndexOf(':');
            if (separator <= 0) return false;

            string cacheGroup = cacheKey.Substring(0, separator);
            string entity = cacheKey.Substring(separator + 1);

            if (!_routes.TryGetValue(cacheGroup, out var evict)) return false;
            evict(container, entity);
            return true;
        }
    }
}
