using Bee.Definition.Settings;
using Bee.Api.Core.Authorization;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Transformers;

namespace Bee.Api.Core
{
    /// <summary>
    /// Provides customizable component settings for the JSON-RPC framework, including authorization validation, data transformation, and serialization strategies.
    /// Users can configure alternative implementations at application startup to meet custom requirements.
    /// </summary>
    public static class ApiServiceOptions
    {
        private static IApiAuthorizationValidator s_authorizationValidator = new ApiAuthorizationValidator(); // Default implementation
        private static IApiPayloadTransformer s_payloadTransformer = new ApiPayloadTransformer(); // Default implementation
        private static IApiPayloadSerializer s_payloadSerializer = new MessagePackPayloadSerializer(); // Default implementation
        private static IApiPayloadCompressor s_payloadCompressor = new GzipPayloadCompressor(); // Default implementation
        private static IApiPayloadEncryptor s_payloadEncryptor = new AesPayloadEncryptor(); // Default implementation
        private static TimeSpan s_wireFrameTimestampTolerance = TimeSpan.FromMinutes(5);
        private static IReplayWindowStore s_replayWindowStore = new MemoryReplayWindowStore();
        /// <summary>
        /// The body codecs a request may name, always all of them.
        /// </summary>
        /// <remarks>
        /// Not a deployment setting. Both are the framework's own, both decode into the same
        /// whitelisted types under the same depth limit, and System.Text.Json is already reachable
        /// by an anonymous caller regardless — the envelope is JSON, and a Plain body is
        /// deserialized by <see cref="Conversion.ApiInputConverter"/>. Gating the JSON body codec
        /// behind a switch would have guarded a door that is open either way, while making a
        /// browser client's support depend on a setting someone has to remember to turn on.
        /// </remarks>
        private static readonly IReadOnlyDictionary<string, IApiPayloadSerializer> s_codecs =
            new Dictionary<string, IApiPayloadSerializer>(StringComparer.Ordinal)
            {
                [PayloadCodecNames.MessagePack] = new MessagePackPayloadSerializer(),
                [PayloadCodecNames.Json] = new JsonPayloadSerializer()
            };

        /// <summary>
        /// Initializes the API service options by configuring the compressor and encryptor implementations.
        /// </summary>
        /// <remarks>
        /// The body codec is not configured here: both built-in codecs are always available and a
        /// request names the one it speaks. <see cref="PayloadSerializer"/> stays at its default
        /// and serves requests that name none.
        /// </remarks>
        /// <param name="payloadOptions">Provides options related to API payload processing, such as serialization, compression, and encryption.</param>
        /// <param name="isDebugMode">
        /// Whether the host is running in debug/development mode. Forwarded to
        /// <see cref="ApiPayloadOptionsFactory.CreateEncryptor(string, bool)"/> so the
        /// <c>"none"</c> encryptor is rejected in production builds.
        /// </param>
        public static void Initialize(ApiPayloadOptions payloadOptions, bool isDebugMode)
        {
            PayloadCompressor = ApiPayloadOptionsFactory.CreateCompressor(payloadOptions.Compressor);
            PayloadEncryptor = ApiPayloadOptionsFactory.CreateEncryptor(payloadOptions.Encryptor, isDebugMode);
        }

        /// <summary>
        /// Initializes the API payload encoding components by directly specifying the serializer, compressor, and encryptor implementations.
        /// This overload can replace the default factory-based creation and is suitable for advanced customization scenarios.
        /// </summary>
        /// <remarks>
        /// WARNING: <paramref name="serializer"/> is not "the codec this deployment uses" — the codec
        /// is declared per request and the server answers with the same one (ADR-044). What this sets
        /// is the codec a request that declares <b>none</b> is read as, and that answer is a
        /// compatibility constant: every client predating negotiation sends MessagePack without
        /// saying so. Installing a different serializer here therefore reads those clients' bodies
        /// with a codec they did not use, and the failure is at deserialization time on the server,
        /// far from this call.
        /// <para>
        /// The legitimate use is installing a serializer the framework does not ship, whose name
        /// <see cref="ResolvePayloadSerializer"/> then also accepts. Swapping between the built-in
        /// codecs is not a use for it.
        /// </para>
        /// </remarks>
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
            get => s_authorizationValidator;
            set => s_authorizationValidator = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the API payload transformer, which provides data encryption, decryption, serialization, and compression.
        /// </summary>
        public static IApiPayloadTransformer PayloadTransformer
        {
            get => s_payloadTransformer;
            set => s_payloadTransformer = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the payload serializer for the API transport layer.
        /// </summary>
        public static IApiPayloadSerializer PayloadSerializer
        {
            get => s_payloadSerializer;
            set => s_payloadSerializer = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the payload compressor for the API transport layer.
        /// </summary>
        public static IApiPayloadCompressor PayloadCompressor
        {
            get => s_payloadCompressor;
            set => s_payloadCompressor = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the payload encryptor for the API transport layer.
        /// </summary>
        public static IApiPayloadEncryptor PayloadEncryptor
        {
            get => s_payloadEncryptor;
            set => s_payloadEncryptor = value ?? throw new ArgumentNullException(nameof(value));
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
            get => s_wireFrameTimestampTolerance;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), "The tolerance must be greater than zero.");
                s_wireFrameTimestampTolerance = value;
            }
        }

        /// <summary>
        /// Gets or sets the store holding each session's sequence-number window.
        /// </summary>
        /// <remarks>
        /// The default keeps windows in this process's memory, which is what a single-node
        /// deployment wants. Behind a load balancer without token affinity each node keeps its own,
        /// so a captured packet can be replayed once per node — still bounded, but a deployment
        /// that needs better can substitute a shared implementation here.
        /// </remarks>
        public static IReplayWindowStore ReplayWindowStore
        {
            get => s_replayWindowStore;
            set => s_replayWindowStore = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets the names of the body codecs a request may ask for.
        /// </summary>
        /// <remarks>
        /// WARNING: this must agree with <see cref="ResolvePayloadSerializer"/>, which also accepts
        /// the name a custom <see cref="PayloadSerializer"/> reports. Listing only the built-in
        /// registry told a deployment that had installed its own codec that the codec it accepts
        /// does not exist — and this list is what a client is meant to negotiate against.
        /// </remarks>
        public static IReadOnlyCollection<string> AcceptedPayloadCodecs
        {
            get
            {
                string custom = PayloadSerializer.SerializationMethod;
                if (s_codecs.ContainsKey(custom)) { return (IReadOnlyCollection<string>)s_codecs.Keys; }
                return [.. s_codecs.Keys, custom];
            }
        }

        /// <summary>
        /// Resolves the body codec a payload asked for by name.
        /// </summary>
        /// <param name="codec">
        /// The codec name read off the payload envelope. Blank means the payload named none, which
        /// is what every client predating negotiation sends, and resolves to
        /// <see cref="PayloadSerializer"/>.
        /// </param>
        /// <returns>The serializer to encode or decode the body with.</returns>
        /// <exception cref="NotSupportedException">
        /// The payload named a codec that does not exist.
        /// </exception>
        /// <remarks>
        /// WARNING: <paramref name="codec"/> arrives from the wire, so it is screened for shape
        /// before it is echoed anywhere. A name that is not well-formed is refused without being
        /// repeated back, which keeps arbitrary caller-supplied text out of the error surface and
        /// out of anything that records it.
        /// </remarks>
        public static IApiPayloadSerializer ResolvePayloadSerializer(string? codec)
        {
            if (string.IsNullOrEmpty(codec))
                return PayloadSerializer;

            if (!IsWellFormedCodecName(codec))
                throw new NotSupportedException("The requested payload codec name is not valid.");

            // A custom serializer installed through the component overload answers to its own
            // name, which is not one of the built-in two.
            if (string.Equals(codec, PayloadSerializer.SerializationMethod, StringComparison.Ordinal))
                return PayloadSerializer;

            if (s_codecs.TryGetValue(codec, out var serializer))
                return serializer;

            throw new NotSupportedException($"Unknown payload codec '{codec}'.");
        }

        /// <summary>
        /// Whether a codec name is shaped like one at all: lower-case letters, digits and hyphens,
        /// up to 32 characters.
        /// </summary>
        private static bool IsWellFormedCodecName(string codec)
        {
            if (codec.Length > 32)
                return false;

            foreach (var c in codec)
            {
                if (!(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a summary of the current settings, including the active serializer, compressor, and encryptor.
        /// </summary>
        public static string CurrentSettingsSummary =>
            $"Serializer: {PayloadSerializer.SerializationMethod}, " +
            $"Codecs: {string.Join('|', AcceptedPayloadCodecs)}, " +
            $"Compressor: {PayloadCompressor.CompressionMethod}, " +
            $"Encryptor: {PayloadEncryptor.EncryptionMethod}";
    }
}
