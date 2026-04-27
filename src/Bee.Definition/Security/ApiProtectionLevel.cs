namespace Bee.Definition.Security
{
    /// <summary>
    /// API access protection level.
    /// </summary>
    public enum ApiProtectionLevel
    {
        /// <summary>
        /// Public: allows any call without enforced encoding (open to third parties).
        /// </summary>
        Public = 0,
        /// <summary>
        /// Encoded: allows remote calls but requires encoding (serialization and compression).
        /// </summary>
        Encoded = 1,
        /// <summary>
        /// Encrypted: allows remote calls but requires encoding and encryption (serialization, compression, and encryption).
        /// </summary>
        Encrypted = 2,
        /// <summary>
        /// Local only: no encoding validation required; suitable for tools and background services.
        /// </summary>
        LocalOnly = 3
    }
}
