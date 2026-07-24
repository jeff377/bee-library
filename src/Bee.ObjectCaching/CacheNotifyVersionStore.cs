using System.Collections.Concurrent;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// In-memory <see cref="ICacheNotifyVersionStore"/> backed by a concurrent dictionary.
    /// </summary>
    /// <remarks>
    /// Versions are per-process observations, not authoritative state — the notify table remains the
    /// source of truth. An unobserved key reports <c>0</c>, which makes a freshly started process
    /// treat every entry as current until the first poll completes.
    /// </remarks>
    public sealed class CacheNotifyVersionStore : ICacheNotifyVersionStore
    {
        private readonly ConcurrentDictionary<string, long> _versions =
            new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public long GetVersion(string notifyKey)
            => _versions.TryGetValue(notifyKey, out long version) ? version : 0L;

        /// <inheritdoc/>
        public void SetVersion(string notifyKey, long version)
            => _versions[notifyKey] = version;
    }
}
