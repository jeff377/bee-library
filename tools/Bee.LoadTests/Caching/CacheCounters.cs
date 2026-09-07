namespace Bee.LoadTests.Caching
{
    /// <summary>
    /// An immutable snapshot of the counts collected by <see cref="CountingCacheProvider"/>.
    /// </summary>
    /// <param name="Hits">Reads that returned a value.</param>
    /// <param name="Misses">Reads that returned no value.</param>
    /// <param name="Writes">Values written to the cache.</param>
    /// <param name="Removals">Entries removed from the cache.</param>
    public readonly record struct CacheCounters(long Hits, long Misses, long Writes, long Removals)
    {
        /// <summary>
        /// Gets the total number of reads.
        /// </summary>
        public long Reads => Hits + Misses;

        /// <summary>
        /// Gets the fraction of reads that were served from the cache, from 0 to 1.
        /// Returns zero when no read happened, which is not the same as a 0% hit rate —
        /// check <see cref="Reads"/> before reading anything into this value.
        /// </summary>
        public double HitRate => Reads == 0 ? 0 : (double)Hits / Reads;
    }
}
