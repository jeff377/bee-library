using Bee.ObjectCaching;
using Bee.ObjectCaching.Providers;

namespace Bee.LoadTests.Caching
{
    /// <summary>
    /// Wraps an <see cref="ICacheProvider"/> and counts reads, hits and writes, so a load test
    /// can report cache behaviour without the framework carrying any instrumentation of its own.
    /// Install it by wrapping whatever <c>CacheInfo.Provider</c> already holds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Write counts answer a question latency cannot: <c>CacheSingleFlight</c> sits above the
    /// provider and collapses concurrent misses on one key into a single creation, and only the
    /// surviving creation reaches <see cref="Set"/>. N virtual users racing for the same cold key
    /// should therefore produce one write, not N.
    /// </para>
    /// <para>
    /// NOTE: this sees provider traffic only. A lookup short-circuited above the provider — the
    /// negative-cache path in <c>KeyObjectCache</c> is the one to know about — never reaches here
    /// and is absent from both the hit and the miss count.
    /// </para>
    /// </remarks>
    public sealed class CountingCacheProvider : ICacheProvider
    {
        private readonly ICacheProvider _inner;
        private long _hits;
        private long _misses;
        private long _writes;
        private long _removals;

        /// <summary>
        /// Initializes a new instance wrapping the given provider.
        /// </summary>
        /// <param name="inner">The provider to delegate to.</param>
        public CountingCacheProvider(ICacheProvider inner)
            => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        /// <summary>
        /// Gets a snapshot of the counts collected so far. The four counters are read
        /// separately, so a snapshot taken while the load is running can be internally
        /// inconsistent; take it after the run.
        /// </summary>
        /// <returns>The counts at the time of the call.</returns>
        public CacheCounters Snapshot() => new(
            Interlocked.Read(ref _hits),
            Interlocked.Read(ref _misses),
            Interlocked.Read(ref _writes),
            Interlocked.Read(ref _removals));

        /// <summary>
        /// Resets every counter to zero. Call between the warm-up and the measured period so
        /// the reported hit rate describes steady state rather than the cold start.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _hits, 0);
            Interlocked.Exchange(ref _misses, 0);
            Interlocked.Exchange(ref _writes, 0);
            Interlocked.Exchange(ref _removals, 0);
        }

        /// <inheritdoc/>
        public bool Contains(string key) => _inner.Contains(key);

        /// <inheritdoc/>
        public object? Get(string key)
        {
            var value = _inner.Get(key);
            if (value is null)
            {
                Interlocked.Increment(ref _misses);
            }
            else
            {
                Interlocked.Increment(ref _hits);
            }
            return value;
        }

        /// <inheritdoc/>
        public void Set(string key, object value, CacheItemPolicy policy)
        {
            Interlocked.Increment(ref _writes);
            _inner.Set(key, value, policy);
        }

        /// <inheritdoc/>
        public void Remove(string key)
        {
            Interlocked.Increment(ref _removals);
            _inner.Remove(key);
        }

        /// <inheritdoc/>
        public long GetCount() => _inner.GetCount();
    }
}
