namespace Bee.Db.Schema
{
    /// <summary>
    /// Options controlling the behavior of the table upgrade orchestrator.
    /// </summary>
    public class UpgradeOptions
    {
        /// <summary>
        /// When true, allows ALTER COLUMN with reduced length or precision that may cause data truncation.
        /// Default is false: narrowing changes are rejected to avoid silent data loss.
        /// </summary>
        public bool AllowColumnNarrowing { get; set; } = false;

        /// <summary>
        /// Gets a shared default instance (all options at their default values).
        /// </summary>
        public static UpgradeOptions Default { get; } = new UpgradeOptions();
    }
}
