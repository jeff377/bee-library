using System;

namespace Bee.Connect
{
    /// <summary>
    /// Application-level runtime configuration for the API client, shared across WinForms, Web, and App targets.
    /// Contains only application-level and connection settings; does not hold user session state.
    /// </summary>
    public class ApiClientContext
    {
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
        public static string ApiKey { get; set; } = "46CA0967-EC64-4F96-B502-139BE8FF8DAC";

        /// <summary>
        /// Gets or sets the API transmission encryption key, exchanged via RSA public key.
        /// Typically unused in local connection scenarios.
        /// </summary>
        public static byte[] ApiEncryptionKey { get; set; } = Array.Empty<byte>();

    }
}
