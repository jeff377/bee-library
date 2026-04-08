using System;
using Bee.Core;

using Bee.Api.Core;

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
        public static void TransformTo(ApiPayload payload, PayloadFormat targetFormat, byte[] encryptionKey = null)
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
        public static void RestoreFrom(ApiPayload payload, PayloadFormat sourceFormat, byte[] encryptionKey = null)
        {
            if (sourceFormat == PayloadFormat.Plain)
            {
                payload.Format = PayloadFormat.Plain;
                return;
            }

            if (string.IsNullOrEmpty(payload.TypeName))
                throw new InvalidOperationException("TypeName is missing for deserialization.");

            var type = Type.GetType(payload.TypeName);
            if (type == null)
                throw new InvalidOperationException("Unable to load type: " + payload.TypeName);

            var bytes = payload.Value as byte[];
            if (bytes == null)
                throw new InvalidCastException("Payload.Value must be byte[].");

            var transformer = ApiServiceOptions.PayloadTransformer;

            if (sourceFormat == PayloadFormat.Encrypted)
            {
                if (BaseFunc.IsEmpty(encryptionKey))
                    throw new InvalidOperationException("Missing encryption key for encrypted payload.");

                bytes = transformer.Decrypt(bytes, encryptionKey);
            }

            payload.Value = transformer.Decode(bytes, type);
            payload.Format = PayloadFormat.Plain;
        }
    }

}
