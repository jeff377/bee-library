using System.Collections.Concurrent;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Collapses concurrent misses on the same cache key into a single creation.
    /// </summary>
    /// <remarks>
    /// <see cref="Providers.ICacheProvider"/> is a plain key-value store with no atomic
    /// get-or-create, and a distributed provider could not offer one without also owning the
    /// creation logic. The de-duplication therefore lives here, next to the
    /// <c>CreateInstance</c> overrides it guards, and applies to every provider.
    /// <para>
    /// WARNING: the map holds only <b>in-flight</b> creations — each entry is removed in a
    /// <c>finally</c>. Do not turn it into a per-key lock table: keys include attacker-supplied
    /// values such as access tokens, so a table that only ever grows is unbounded.
    /// </para>
    /// <para>
    /// The factory runs outside any shared lock, so two different keys never wait on each other
    /// and no lock ordering exists between caches. Re-entering the same key on the same thread
    /// throws from <see cref="Lazy{T}"/> rather than deadlocking.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The cached object type.</typeparam>
    internal static class CacheSingleFlight<T> where T : class
    {
        private static readonly ConcurrentDictionary<string, Lazy<T?>> s_inFlight =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Runs <paramref name="factory"/> for <paramref name="cacheKey"/>, or waits for the
        /// creation already running for that key and returns its result.
        /// </summary>
        /// <param name="cacheKey">The fully qualified cache key, including any owner prefix.</param>
        /// <param name="factory">
        /// Creates the value and writes it to the cache. Runs at most once per flight; callers that
        /// arrive while it runs receive the same instance.
        /// </param>
        public static T? GetOrCreate(string cacheKey, Func<T?> factory)
        {
            // A losing GetOrAdd factory only constructs a Lazy — it never runs `factory`, so the
            // usual "ConcurrentDictionary may invoke the value factory more than once" caveat
            // costs nothing here.
            var flight = s_inFlight.GetOrAdd(
                cacheKey,
                _ => new Lazy<T?>(factory, LazyThreadSafetyMode.ExecutionAndPublication));
            try
            {
                return flight.Value;
            }
            finally
            {
                // Compare-and-remove: never evict a newer flight started after this one finished.
                s_inFlight.TryRemove(new KeyValuePair<string, Lazy<T?>>(cacheKey, flight));
            }
        }
    }
}
