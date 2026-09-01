namespace Bee.Definition.Security
{
    /// <summary>
    /// Whether an API method's calls must each carry a sequence number not seen before.
    /// </summary>
    /// <remarks>
    /// A third dimension alongside <see cref="ApiProtectionLevel"/> and
    /// <see cref="ApiAccessRequirement"/>, declared per method because replaying a read costs
    /// nothing while replaying a write does not.
    /// </remarks>
    public enum ApiReplayProtection
    {
        /// <summary>
        /// Calls are not checked for a repeated sequence number. The default, and correct for reads.
        /// </summary>
        None = 0,

        /// <summary>
        /// Each call must carry a sequence number this session has not used before.
        /// </summary>
        /// <remarks>
        /// Effective only where the caller actually sends a frame — an authenticated session over
        /// Encoded or Encrypted, with the wire frame switched on. A Plain call carries no frame and
        /// so is not checked; that gap is a known limitation of leaving write methods at
        /// <see cref="ApiProtectionLevel.Public"/>, not something this setting can close.
        /// Anonymous callers are not checked either: sequence numbers are per session, and calls
        /// made without one have no session to count against.
        /// </remarks>
        UniqueSequence = 1
    }
}
