namespace Bee.ObjectCaching
{
    /// <summary>
    /// Cache condition dependency time type.
    /// </summary>
    public enum CacheTimeKind : int
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
