using Bee.Base;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Utility class for handling <see cref="ApiPayload"/> format conversion (serialization, compression, and encryption).
    /// </summary>
    public static class ApiPayloadConverter
    {
        /// <summary>
        /// Converts the specified payload object to the target format (encoded or encrypted).
        /// </summary>
        /// <param name="payload">The payload object to convert.</param>
        /// <param name="targetFormat">The target format, such as Encoded or Encrypted.</param>
        /// <param name="encryptionKey">The encryption key; required only when <paramref name="targetFormat"/> is Encrypted.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="targetFormat"/> is Encrypted but no key is provided, or when Payload.Value is null.
        /// </exception>
        /// <remarks>
        /// When <see cref="ApiServiceOptions.RequireWireFrame"/> is on, an anti-replay frame is
        /// packed in front of the encoded body. Plain payloads are returned untouched and never
        /// carry one.
        /// </remarks>
        public static void TransformTo(ApiPayload payload, PayloadFormat targetFormat, byte[]? encryptionKey = null)
        {
            if (targetFormat == PayloadFormat.Plain)
            {
                payload.Format = PayloadFormat.Plain;
                return;
            }

            if (payload.Value == null)
                throw new InvalidOperationException("Payload.Value cannot be null.");

            var type = payload.Value.GetType();
            payload.TypeName = type.FullName + ", " + type.Assembly.GetName().Name;

            var transformer = ApiServiceOptions.PayloadTransformer;
            var bytes = transformer.Encode(payload.Value, type);

            if (ApiServiceOptions.RequireWireFrame)
            {
                // Prepend after encoding and before encryption, so the payload HMAC covers the frame.
                var frame = payload.Frame ?? new ApiPayloadFrame(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sequence: 0);
                payload.Frame = frame;
                bytes = frame.Prepend(bytes);
            }

            if (targetFormat == PayloadFormat.Encrypted)
            {
                if (encryptionKey == null || encryptionKey.Length == 0)
                    throw new InvalidOperationException("Encryption key is required for encrypted payload.");

                bytes = transformer.Encrypt(bytes, encryptionKey);
            }

            payload.Value = bytes;
            payload.Format = targetFormat;
        }

        /// <summary>
        /// Restores the specified payload object from its encoded or encrypted format back to the original object.
        /// </summary>
        /// <param name="payload">The payload object to restore.</param>
        /// <param name="sourceFormat">The source format; should be Encoded or Encrypted.</param>
        /// <param name="encryptionKey">The decryption key; required only when <paramref name="sourceFormat"/> is Encrypted.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="sourceFormat"/> is Encrypted but no key is provided, or when TypeName cannot be resolved.
        /// </exception>
        /// <exception cref="InvalidCastException">Thrown when Payload.Value is not of type byte[].</exception>
        /// <exception cref="ReplayRejectedException">
        /// Thrown when <see cref="ApiServiceOptions.RequireWireFrame"/> is on but the payload carries
        /// no readable frame — most often a client older than that requirement.
        /// </exception>
        /// <remarks>
        /// The frame that was read is left on <see cref="ApiPayload.Frame"/> for the caller to check.
        /// </remarks>
        public static void RestoreFrom(ApiPayload payload, PayloadFormat sourceFormat, byte[]? encryptionKey = null)
        {
            if (sourceFormat == PayloadFormat.Plain)
            {
                payload.Format = PayloadFormat.Plain;
                return;
            }

            if (string.IsNullOrEmpty(payload.TypeName))
                throw new InvalidOperationException("TypeName is missing for deserialization.");

            // Validate TypeName against the allowed type whitelist before loading the type.
            // TypeName format: "Namespace.TypeName, AssemblyName"
            ValidateTypeName(payload.TypeName);

            var type = Type.GetType(payload.TypeName);
            if (type == null)
                throw new InvalidOperationException("Unable to load type: " + payload.TypeName);

            var bytes = payload.Value as byte[];
            if (bytes == null)
                throw new InvalidCastException("Payload.Value must be byte[].");

            var transformer = ApiServiceOptions.PayloadTransformer;

            if (sourceFormat == PayloadFormat.Encrypted)
            {
                if (encryptionKey == null || ValueUtilities.IsEmpty(encryptionKey))
                    throw new InvalidOperationException("Missing encryption key for encrypted payload.");

                bytes = transformer.Decrypt(bytes, encryptionKey);
            }

            if (ApiServiceOptions.RequireWireFrame)
            {
                // Whether a frame is expected is a deployment decision, never read from the packet:
                // letting a request declare "I carry no frame" would be a downgrade attack.
                payload.Frame = ApiPayloadFrame.Extract(bytes, out bytes);
            }

            payload.Value = transformer.Decode(bytes, type);
            payload.Format = PayloadFormat.Plain;
        }

        /// <summary>
        /// Validates that the TypeName is in the allowed type whitelist.
        /// Prevents arbitrary type loading from client-supplied type names.
        /// </summary>
        /// <param name="typeName">
        /// The assembly-qualified type name (e.g., "Bee.Api.Core.Messages.System.LoginRequest, Bee.Api.Core").
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown when the type is not in the allowed whitelist.</exception>
        private static void ValidateTypeName(string typeName)
        {
            // WARNING: Screen the whole assembly-qualified name, generic arguments included. Do not
            // reduce this to "take everything before the first comma" — for a generic type that
            // comma sits inside `[[...]]`, so the fragment still starts with an allowed namespace
            // and the argument reaches `Type.GetType` unscreened.
            if (!WireTypeWhitelist.IsAssemblyQualifiedNameAllowed(typeName))
            {
                throw new InvalidOperationException(
                    $"Payload type '{typeName}' is not in the allowed type whitelist.");
            }
        }
    }

}
