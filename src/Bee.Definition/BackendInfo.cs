using Bee.Definition.Logging;

namespace Bee.Definition
{
    /// <summary>
    /// Transitional holder for backend-wide configuration values. Phase 4 reduced this
    /// class to four encryption keys + <see cref="LogOptions"/> + <see cref="LogWriter"/>;
    /// Phase 5/6 will move the remaining fields to <c>IOptions&lt;T&gt;</c> and remove the
    /// class entirely.
    /// </summary>
    public static class BackendInfo
    {
        /// <summary>
        /// Gets or sets the log writer.
        /// </summary>
        public static ILogWriter LogWriter { get; set; } = new NullLogWriter();

        /// <summary>
        /// Gets or sets the logging options for configuring log-related parameters.
        /// </summary>
        public static LogOptions LogOptions { get; set; } = new LogOptions();

        /// <summary>
        /// Gets or sets the API transport encryption key.
        /// </summary>
        public static byte[] ApiEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the Cookie data encryption key.
        /// </summary>
        public static byte[] CookieEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the encryption key for sensitive data in configuration files.
        /// </summary>
        public static byte[] ConfigEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the encryption key for sensitive database fields.
        /// </summary>
        public static byte[] DatabaseEncryptionKey { get; set; } = Array.Empty<byte>();
    }
}
