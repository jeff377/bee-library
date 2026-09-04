namespace Bee.Api.Core.Transformers
{
    /// <summary>
    /// Factory class that creates API payload encoding components based on configuration values.
    /// </summary>
    /// <remarks>
    /// NOTE: there is deliberately no <c>CreateSerializer</c> here. The body codec is <b>not</b> a
    /// deployment setting — each request declares it on the payload envelope and the server answers
    /// with the same one (ADR-044). A factory taking a codec name kept implying otherwise, and the
    /// implication was actively dangerous: feeding its result to
    /// <see cref="Bee.Api.Core.ApiServiceOptions.Initialize(Bee.Api.Core.Transformers.IApiPayloadSerializer, Bee.Api.Core.Transformers.IApiPayloadCompressor, Bee.Api.Core.Transformers.IApiPayloadEncryptor)"/>
    /// changes what a request that declares <i>no</i> codec is read as, which silently breaks every
    /// client predating negotiation. Compressor and encryptor stay, because those two really are
    /// deployment settings.
    /// </remarks>
    public static class ApiPayloadOptionsFactory
    {
        /// <summary>
        /// Creates the compressor component with the specified name.
        /// </summary>
        /// <param name="name">The compressor name, e.g., "gzip", or "none" for no compression.</param>
        /// <returns>The compressor component.</returns>
        /// <exception cref="NotSupportedException">The compressor name is not supported.</exception>
        public static IApiPayloadCompressor CreateCompressor(string name)
        {
            switch (name)
            {
                case "gzip":
                    return new GzipPayloadCompressor();
                case "none":
                case "":
                    return new NoCompressionCompressor();
                default:
                    throw new NotSupportedException($"Unsupported compressor: {name}");
            }
        }

        /// <summary>
        /// Creates the encryptor component with the specified name.
        /// </summary>
        /// <param name="name">The encryptor name, e.g., "aes-cbc-hmac", or "none" for no encryption.</param>
        /// <param name="isDebugMode">
        /// Whether the host is running in debug/development mode. Required: <c>"none"</c> /
        /// empty-string encryptors are only permitted when this flag is <c>true</c> so that
        /// production deployments cannot accidentally disable transport encryption.
        /// </param>
        /// <returns>The encryptor component.</returns>
        /// <exception cref="NotSupportedException">The encryptor name is not supported.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="name"/> is <c>"none"</c> / empty and <paramref name="isDebugMode"/> is <c>false</c>.</exception>
        public static IApiPayloadEncryptor CreateEncryptor(string name, bool isDebugMode)
        {
            switch (name)
            {
                case "aes-cbc-hmac":
                    return new AesPayloadEncryptor();
                case "none":
                case "":
                    if (!isDebugMode)
                        throw new InvalidOperationException(
                            "NoEncryptionEncryptor is only permitted in debug/development mode. Configure a valid encryptor for production.");
                    return new NoEncryptionEncryptor();
                default:
                    throw new NotSupportedException($"Unsupported encryptor: {name}");
            }
        }
    }
}
