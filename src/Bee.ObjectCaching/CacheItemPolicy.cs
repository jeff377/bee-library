using System;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Cache item expiration policy.
    /// </summary>
    public class CacheItemPolicy
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItemPolicy"/> class.
        /// </summary>
        public CacheItemPolicy()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItemPolicy"/> class with a time-based expiration.
        /// </summary>
        /// <param name="kind">The time type for the expiration condition. Only one of AbsoluteExpiration or SlidingExpiration can be set.</param>
        /// <param name="minutes">The number of minutes until expiration.</param>
        public CacheItemPolicy(CacheTimeKind  kind, int minutes)
        {
            if (kind == CacheTimeKind.AbsoluteTime)
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(minutes);  // Absolute time expiration
            else
                SlidingExpiration = TimeSpan.FromMinutes(minutes);  // Sliding time expiration
        }

        #endregion

        /// <summary>
        /// Gets or sets the absolute expiration time, indicating when the cache item should be evicted.
        /// </summary>
        public DateTimeOffset AbsoluteExpiration { get; set; } = DateTimeOffset.MaxValue;

        /// <summary>
        /// Gets or sets the sliding expiration duration, indicating whether cache items that have not been accessed for a period of time should be evicted.
        /// </summary>
        public TimeSpan SlidingExpiration { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the array of directory and file paths to monitor for changes.
        /// </summary>
        public string[]? ChangeMonitorFilePaths { get; set; } = null;

        /// <summary>
        /// Gets or sets the array of change-monitor keys for the ST_Cache database table.
        /// </summary>
        public string[]? ChangeMonitorDbKeys { get; set; } = null;
    }
}
