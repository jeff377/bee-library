namespace Bee.Cache
{
    /// <summary>
    /// Cache condition dependency time type.
    /// </summary>
    public enum CacheTimeKind
    {
        /// <summary>
        /// Sliding expiration time.
        /// </summary>
        SlidingTime,
        /// <summary>
        /// Absolute expiration time.
        /// </summary>
        AbsoluteTime
    }
}
