namespace Bee.Definition.Logging
{
    /// <summary>
    /// Classification of an execution anomaly — an execution that deviated from the normal /
    /// expected envelope, whether it failed or merely ran outside a threshold.
    /// </summary>
    public enum AnomalyKind
    {
        /// <summary>An exception occurred — needs a bug fix.</summary>
        Error = 1,

        /// <summary>The operation timed out — an infrastructure / performance signal, not a code bug.</summary>
        Timeout = 2,

        /// <summary>Completed, but took longer than the configured warning threshold.</summary>
        Slow = 3,

        /// <summary>(DB) Affected more rows than the configured threshold.</summary>
        LargeAffected = 4,

        /// <summary>(DB) Returned more rows than the configured threshold.</summary>
        LargeResult = 5,
    }
}
