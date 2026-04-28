using Bee.Base;

namespace Bee.Api.Core.Transformers
{
    /// <summary>
    /// Factory class that creates API payload encoding components based on configuration values.
    /// </summary>
    public static class ApiPayloadOptionsFactory
    {
        /// <summary>
        /// Creates the serializer component with the specified name.
        /// </summary>
        /// <param name="name">The serializer name, e.g., "messagepack".</param>
        /// <returns>The serializer component.</returns>
        /// <exception cref="NotSupportedException">The serializer name is not supported.</exception>
        public static IApiPayloadSerializer CreateSerializer(string name)
        {
            switch (name)
            {
                case "messagepack":
                    return new MessagePackPayloadSerializer();
                default:
                    throw new NotSupportedException($"Unsupported serializer: {name}");
            }
        }

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
        /// <returns>The encryptor component.</returns>
        /// <exception cref="NotSupportedException">The encryptor name is not supported.</exception>
        public static IApiPayloadEncryptor CreateEncryptor(string name)
        {
            switch (name)
            {
                case "aes-cbc-hmac":
                    return new AesPayloadEncryptor();
                case "none":
                case "":
                    if (!SysInfo.IsDebugMode)
                        throw new InvalidOperationException(
                            "NoEncryptionEncryptor is only permitted in debug/development mode. Configure a valid encryptor for production.");
                    return new NoEncryptionEncryptor();
                default:
                    throw new NotSupportedException($"Unsupported encryptor: {name}");
            }
        }
    }
}
