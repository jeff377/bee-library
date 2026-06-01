using System.ComponentModel;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Polling options for the database-backed cache notification mechanism
    /// (the <c>st_cache_notify</c> table). Consumed by the cache-notify poller hosted service.
    /// </summary>
    [Description("Polling options for the database-backed cache notification mechanism.")]
    [TreeNode("CacheNotify")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class CacheNotifyOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the cache-notify poller is enabled.
        /// Disable for a pure single-node deployment where every write goes through this
        /// process and in-process eviction already covers invalidation.
        /// </summary>
        [Category("CacheNotify")]
        [Description("Whether the cache-notify poller is enabled.")]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the polling interval in seconds. The poller reads incremental changes
        /// from the notification table once per interval.
        /// </summary>
        [Category("CacheNotify")]
        [Description("Polling interval in seconds.")]
        [DefaultValue(5)]
        public int IntervalSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the safety margin in seconds. Each poll re-reads rows whose update time is
        /// within <c>highWater - margin</c>, so a long transaction whose <c>sys_update_time</c>
        /// precedes its commit visibility is not missed at the boundary. Version comparison keeps the
        /// overlap idempotent.
        /// </summary>
        [Category("CacheNotify")]
        [Description("Safety margin in seconds for the incremental read overlap window.")]
        [DefaultValue(5)]
        public int MarginSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the database identifier whose <c>st_cache_notify</c> table is polled.
        /// Defaults to the conventional <c>common</c> database.
        /// </summary>
        [Category("CacheNotify")]
        [Description("Database identifier whose notification table is polled.")]
        [DefaultValue("common")]
        public string DatabaseId { get; set; } = "common";

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
