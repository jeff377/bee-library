namespace Bee.Api.Client
{
    /// <summary>
    /// The per-session client state a connector needs: the transmission key established at login and
    /// the signed-in user's time zone.
    /// </summary>
    /// <remarks>
    /// These two values used to live on <see cref="ApiClientInfo"/> as process-wide statics, which is
    /// correct for a desktop head — one process, one user — and wrong for a host that serves several
    /// users from one process. <c>Bee.Web.Blazor.Server</c> is such a host: every circuit runs
    /// <c>LoginAsync</c> against the same statics, so the last login wins and earlier users find their
    /// requests encrypted with someone else's key.
    /// <para>
    /// WARNING: What belongs here is state whose owner is <b>a signed-in user</b>. Deployment-level
    /// settings do not — <see cref="ApiClientInfo.ApiKey"/> identifies the application to the server
    /// and is the same for every circuit, so making it per-session would be wrong, not safer.
    /// </para>
    /// <para>
    /// A connector created without one shares <see cref="Ambient"/>, which keeps every existing
    /// single-user host behaving exactly as before. A multi-user host creates one per session and
    /// passes it to the connector constructors that take it.
    /// </para>
    /// </remarks>
    public sealed class ApiSessionContext
    {
        /// <summary>
        /// The shared instance used by connectors that were not given one.
        /// </summary>
        /// <remarks>
        /// This is the pre-existing process-wide behaviour, kept so a desktop host needs no change.
        /// It is deliberately not the answer for a multi-user host: sharing it there is the defect
        /// this type exists to fix.
        /// </remarks>
        public static ApiSessionContext Ambient { get; } = new ApiSessionContext();

        /// <summary>
        /// Gets or sets the API transmission encryption key, exchanged via RSA public key at login.
        /// Typically unused in local connection scenarios.
        /// </summary>
        public byte[] ApiEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the signed-in user's IANA time zone id; blank disables time zone conversion.
        /// </summary>
        /// <remarks>
        /// Blank is the correct state before sign-in — there is no user whose zone could apply, and
        /// adopting the device's would reintroduce the second source of truth ADR-032 D4 rejects.
        /// </remarks>
        public string UserTimeZoneId { get; set; } = string.Empty;
    }
}
