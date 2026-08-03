using MessagePack;

namespace Bee.Definition.Security
{
    /// <summary>
    /// One issued API key as an operator sees it: everything needed to decide what to disable,
    /// expire or rotate, and nothing that could be used to authenticate.
    /// </summary>
    /// <remarks>
    /// WARNING: this type exists precisely because <see cref="ApiKeyInfo"/> must not leave the
    /// server. That one carries <see cref="ApiKeyInfo.HashedKey"/> — it is the cache's payload for
    /// validating a presented key — and putting a credential hash on the wire would hand every
    /// operator an offline-crackable artefact. Do not add the hash here, and do not widen
    /// <c>ApiKeyInfo</c>'s use to cover listing.
    /// <para>
    /// Unlike <c>ApiKeyInfo</c>, disabled keys are included: managing a key's lifecycle is exactly
    /// the case where the disabled ones matter.
    /// </para>
    /// </remarks>
    [MessagePackObject(keyAsPropertyName: true)]
    public class ApiKeySummary
    {
        /// <summary>
        /// Gets or sets the key identifier (<c>sys_id</c>), which is also the leading segment of the
        /// plaintext key. Not a secret.
        /// </summary>
        public string SysId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the application the key was issued for.
        /// </summary>
        public string SysName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key classification. A label for operators; it carries no authorization
        /// meaning.
        /// </summary>
        public ApiKeyType KeyType { get; set; } = ApiKeyType.Internal;

        /// <summary>
        /// Gets or sets the contact for the third party holding this key.
        /// </summary>
        public string Contact { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the key is currently accepted.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the UTC expiry, or <c>null</c> when the key does not expire.
        /// </summary>
        public DateTime? ExpiredAt { get; set; }

        /// <summary>
        /// Gets or sets the UTC time the key was issued, which is how a rotation tells the
        /// outgoing key from its replacement.
        /// </summary>
        public DateTime? IssuedAt { get; set; }
    }
}
