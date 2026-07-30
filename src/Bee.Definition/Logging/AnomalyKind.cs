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

        /// <summary>
        /// (API) The call was rejected before it ran — a supplied API key was not accepted.
        /// </summary>
        /// <remarks>
        /// Not a code defect: it is the detection signal for a misconfigured client or a probing
        /// caller. With per-key rate limiting deliberately out of scope, these records are the only
        /// place such attempts become visible.
        /// </remarks>
        Unauthorized = 6,
    }
}
