using System.ComponentModel;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Settings for the background job that deletes expired rows from <c>st_session</c>.
    /// </summary>
    /// <remarks>
    /// Sessions are read without side effects, so nothing reclaims an expired row on the request
    /// path. Every sign-in inserts one, which makes this job the only thing keeping the table from
    /// growing without bound. Deleting by expiry time is idempotent, so several nodes running it
    /// at once is safe.
    /// </remarks>
    [Description("Expired session cleanup settings.")]
    [TreeNode("SessionCleanup")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class SessionCleanupOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the cleanup job is enabled.
        /// </summary>
        /// <remarks>
        /// Enabled by default because the table grows on every sign-in; a deployment that reclaims
        /// the rows by its own means can turn it off.
        /// </remarks>
        [Category("SessionCleanup")]
        [Description("Whether the expired session cleanup job is enabled.")]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval in seconds between cleanup passes.
        /// </summary>
        /// <remarks>
        /// Expired rows lingering for one interval is harmless: they can no longer be read back as
        /// a session, and <c>AccessTokenValidator</c> checks the expiry regardless. The interval
        /// therefore trades table size against database load, not correctness.
        /// </remarks>
        [Category("SessionCleanup")]
        [Description("Interval in seconds between cleanup passes.")]
        [DefaultValue(3600)]
        public int IntervalSeconds { get; set; } = 3600;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
