using Bee.Definition.Settings;
using System;
using Bee.Definition;
using Bee.Api.Core.Authorization;
using Bee.Api.Core.Transformer;

namespace Bee.Api.Core
{
    /// <summary>
    /// Provides customizable component settings for the JSON-RPC framework, including authorization validation, data transformation, and serialization strategies.
    /// Users can configure alternative implementations at application startup to meet custom requirements.
    /// </summary>
    public static class ApiServiceOptions
    {
        private static IApiAuthorizationValidator _authorizationValidator = new ApiAuthorizationValidator(); // Default implementation
        private static IApiPayloadTransformer _payloadTransformer = new ApiPayloadTransformer(); // Default implementation
        private static IApiPayloadSerializer _payloadSerializer = new MessagePackPayloadSerializer(); // Default implementation
        private static IApiPayloadCompressor _payloadCompressor = new GZipPayloadCompressor(); // Default implementation
        private static IApiPayloadEncryptor _payloadEncryptor = new AesPayloadEncryptor(); // Default implementation

        /// <summary>
        /// Initializes the API service options by configuring the serializer, compressor, and encryptor implementations.
        /// </summary>
        /// <param name="payloadOptions">Provides options related to API payload processing, such as serialization, compression, and encryption.</param>
        public static void Initialize(ApiPayloadOptions payloadOptions)
        {
            PayloadSerializer = ApiPayloadOptionsFactory.CreateSerializer(payloadOptions.Serializer);
            PayloadCompressor = ApiPayloadOptionsFactory.CreateCompressor(payloadOptions.Compressor);
            PayloadEncryptor = ApiPayloadOptionsFactory.CreateEncryptor(payloadOptions.Encryptor);
        }

        /// <summary>
        /// Initializes the API payload encoding components by directly specifying the serializer, compressor, and encryptor implementations.
        /// This overload can replace the default factory-based creation and is suitable for advanced customization scenarios.
        /// </summary>
        /// <param name="serializer">The custom serializer.</param>
        /// <param name="compressor">The custom compressor.</param>
        /// <param name="encryptor">The custom encryptor.</param>
        public static void Initialize(
            IApiPayloadSerializer serializer,
            IApiPayloadCompressor compressor,
            IApiPayloadEncryptor encryptor)
        {
            PayloadSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            PayloadCompressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
            PayloadEncryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        }


        /// <summary>
        /// Gets or sets the API key and authorization validator.
        /// </summary>
        public static IApiAuthorizationValidator AuthorizationValidator
        {
            get => _authorizationValidator;
            set => _authorizationValidator = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the API payload transformer, which provides data encryption, decryption, serialization, and compression.
        /// </summary>
        public static IApiPayloadTransformer PayloadTransformer
        {
            get => _payloadTransformer;
            set => _payloadTransformer = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the payload serializer for the API transport layer.
        /// </summary>
        public static IApiPayloadSerializer PayloadSerializer
        {
            get => _payloadSerializer;
            set => _payloadSerializer = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the payload compressor for the API transport layer.
        /// </summary>
        public static IApiPayloadCompressor PayloadCompressor
        {
            get => _payloadCompressor;
            set => _payloadCompressor = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the payload encryptor for the API transport layer.
        /// </summary>
        public static IApiPayloadEncryptor PayloadEncryptor
        {
            get => _payloadEncryptor;
            set => _payloadEncryptor = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets a summary of the current settings, including the active serializer, compressor, and encryptor.
        /// </summary>
        public static string CurrentSettingsSummary =>
            $"Serializer: {PayloadSerializer.SerializationMethod}, " +
            $"Compressor: {PayloadCompressor.CompressionMethod}, " +
            $"Encryptor: {PayloadEncryptor.EncryptionMethod}";
    }
}
