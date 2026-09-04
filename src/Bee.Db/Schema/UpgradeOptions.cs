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
        /// <remarks>
        /// WARNING: <c>init</c> rather than <c>set</c>, and that is load-bearing because of
        /// <see cref="Default"/>. A settable property on a shared static instance means any caller
        /// can turn "allow changes that may truncate data" on for the whole process, from anywhere,
        /// permanently — and nothing would report it. Pass a new instance instead:
        /// <c>new UpgradeOptions { AllowColumnNarrowing = true }</c>.
        /// </remarks>
        public bool AllowColumnNarrowing { get; init; } = false;

        /// <summary>
        /// Gets a shared default instance (all options at their default values).
        /// </summary>
        /// <remarks>
        /// Safe to share only because every property here is <c>init</c>-only. Adding a settable
        /// one would turn this into process-wide mutable state wearing the word "Default".
        /// </remarks>
        public static UpgradeOptions Default { get; } = new UpgradeOptions();
    }
}
