using Bee.Definition.Settings;
using Bee.Api.Core.Authorization;
using Bee.Api.Core.Transformers;

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
        private static IApiPayloadCompressor _payloadCompressor = new GzipPayloadCompressor(); // Default implementation
        private static IApiPayloadEncryptor _payloadEncryptor = new AesPayloadEncryptor(); // Default implementation
        private static TimeSpan _wireFrameTimestampTolerance = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Initializes the API service options by configuring the serializer, compressor, and encryptor implementations.
        /// </summary>
        /// <param name="payloadOptions">Provides options related to API payload processing, such as serialization, compression, and encryption.</param>
        /// <param name="isDebugMode">
        /// Whether the host is running in debug/development mode. Forwarded to
        /// <see cref="ApiPayloadOptionsFactory.CreateEncryptor(string, bool)"/> so the
        /// <c>"none"</c> encryptor is rejected in production builds.
        /// </param>
        public static void Initialize(ApiPayloadOptions payloadOptions, bool isDebugMode)
        {
            PayloadSerializer = ApiPayloadOptionsFactory.CreateSerializer(payloadOptions.Serializer);
            PayloadCompressor = ApiPayloadOptionsFactory.CreateCompressor(payloadOptions.Compressor);
            PayloadEncryptor = ApiPayloadOptionsFactory.CreateEncryptor(payloadOptions.Encryptor, isDebugMode);
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
        /// Gets or sets whether Encoded and Encrypted payloads carry an anti-replay frame.
        /// </summary>
        /// <remarks>
        /// <b>Both ends must agree, and both read this same switch.</b> A client writes a frame only
        /// when this is set; a server requires one only when this is set. It is deliberately not
        /// negotiated per packet — letting a request declare "I carry no frame" would be a downgrade
        /// attack — so a mismatched pair fails rather than silently running unprotected.
        /// <para>
        /// Defaults to <c>false</c>, which reproduces the behaviour of builds that predate the frame.
        /// Turning it on is a breaking change for any peer that has not been upgraded: roll the
        /// package out to both ends first, then enable the switch on both.
        /// </para>
        /// <para>
        /// Plain payloads never carry a frame whatever this says — they have no envelope, so nothing
        /// would stop an attacker rewriting the frame's contents.
        /// </para>
        /// </remarks>
        public static bool RequireWireFrame { get; set; }

        /// <summary>
        /// Gets or sets how far an inbound frame's timestamp may drift from server time before the
        /// call is refused. Defaults to five minutes.
        /// </summary>
        /// <remarks>
        /// The window has to absorb client clock skew, and a desktop whose NTP is misconfigured is
        /// routinely minutes out, so tightening this below a minute starts rejecting honest callers.
        /// The cost of the width is that a captured packet stays replayable for that long; closing
        /// that gap is the sequence window's job, not this one's.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to a non-positive value.</exception>
        public static TimeSpan WireFrameTimestampTolerance
        {
            get => _wireFrameTimestampTolerance;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), "The tolerance must be greater than zero.");
                _wireFrameTimestampTolerance = value;
            }
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
