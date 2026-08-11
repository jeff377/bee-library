namespace Bee.Api.Client
{
    /// <summary>
    /// API client runtime information; shared connection parameters and settings for the API client,
    /// across WinForms, Web, and App targets. Provides the client-side counterpart to the backend's
    /// DI-registered services (see <c>Bee.Hosting.BeeFrameworkServiceCollectionExtensions.AddBeeFramework</c>).
    /// Contains only application-level and connection settings; does not hold user session state.
    /// </summary>
    public static class ApiClientInfo
    {
        /// <summary>
        /// Gets or sets the signed-in user's IANA time zone id; blank disables time zone conversion.
        /// </summary>
        /// <remarks>
        /// The Connector converts payloads between UTC and this zone (ADR-032 D4). It lives here
        /// rather than being read from the UI layer because <c>Bee.Api.Client</c> sits below it; the
        /// host assigns it at login and clears it at logout.
        ///
        /// Blank means no conversion, which is the correct state before sign-in — there is no user
        /// whose zone could apply, and adopting the device's would reintroduce the second source of
        /// truth D4 rejects.
        /// </remarks>
        public static string UserTimeZoneId
        {
            get => ApiSessionContext.Ambient.UserTimeZoneId;
            set => ApiSessionContext.Ambient.UserTimeZoneId = value;
        }

        /// <summary>
        /// Gets or sets the connection types supported by the application.
        /// </summary>
        public static SupportedConnectTypes SupportedConnectTypes { get; set; } = SupportedConnectTypes.Both;

        /// <summary>
        /// Gets or sets the active service connection type.
        /// </summary>
        public static ConnectType ConnectType { get; set; } = ConnectType.Local;

        /// <summary>
        /// Gets or sets the API service endpoint, typically loaded from configuration.
        /// </summary>
        public static string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key, typically loaded from configuration.
        /// </summary>
        public static string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API transmission encryption key, exchanged via RSA public key.
        /// Typically unused in local connection scenarios.
        /// </summary>
        public static byte[] ApiEncryptionKey
        {
            get => ApiSessionContext.Ambient.ApiEncryptionKey;
            set => ApiSessionContext.Ambient.ApiEncryptionKey = value;
        }

        /// <summary>
        /// Gets or sets the in-process backend service provider used by <c>LocalApiProvider</c>.
        /// Set this once at startup to the result of
        /// <c>services.AddBeeFramework(configuration).BuildServiceProvider()</c> when the
        /// application wants to execute backend logic in-process.
        /// </summary>
        /// <remarks>
        /// Transitional storage: <c>Bee.Api.Client</c> near-end mode was left out of the DI
        /// migration, so the service provider lives here rather than being constructor-injected.
        /// </remarks>
        public static IServiceProvider? LocalServiceProvider { get; set; }

    }
}
