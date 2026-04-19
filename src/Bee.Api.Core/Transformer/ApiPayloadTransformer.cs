namespace Bee.Api.Core.Transformer
{
    /// <summary>
    /// Default API payload transformer that provides data serialization, compression, and encryption/decryption.
    /// </summary>
    public class ApiPayloadTransformer : IApiPayloadTransformer
    {
        /// <summary>
        /// Serializes and compresses the specified object.
        /// </summary>
        /// <param name="payload">The raw data object to process.</param>
        /// <param name="type">The type of the object.</param>
        /// <returns>The processed data (typically a byte array).</returns>
        public byte[] Encode(object payload, Type type)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload), "Input data cannot be null.");
            }

            try
            {
                byte[] bytes = ApiServiceOptions.PayloadSerializer.Serialize(payload, type);  // Serialize
                return ApiServiceOptions.PayloadCompressor.Compress(bytes);                    // Compress
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred during the data encoding process.", ex);
            }
        }

        /// <summary>
        /// Decompresses and deserializes the processed data back to the original object.
        /// </summary>
        /// <param name="payload">The processed data (typically a byte array).</param>
        /// <param name="type">The target object type.</param>
        /// <returns>The restored original data object.</returns>
        public object? Decode(object payload, Type type)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload), "Input data cannot be null.");
            }

            try
            {
                byte[]? bytes = payload as byte[];
                if (bytes == null)
                {
                    throw new InvalidCastException("Invalid data type. The input data must be a byte array.");
                }

                byte[] decompressed = ApiServiceOptions.PayloadCompressor.Decompress(bytes);       // Decompress
                return ApiServiceOptions.PayloadSerializer.Deserialize(decompressed, type);        // Deserialize
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred during the data decoding process.", ex);
            }
        }

        /// <summary>
        /// Encrypts the specified byte data only.
        /// </summary>
        /// <param name="rawBytes">The raw byte data.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <returns>The encrypted data.</returns>
        public byte[] Encrypt(byte[] rawBytes, byte[] encryptionKey)
        {
            if (rawBytes == null)
            {
                throw new ArgumentNullException(nameof(rawBytes), "Raw data cannot be null.");
            }

            return ApiServiceOptions.PayloadEncryptor.Encrypt(rawBytes, encryptionKey);
        }

        /// <summary>
        /// Decrypts the specified byte data only.
        /// </summary>
        /// <param name="encryptedBytes">The encrypted data.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <returns>The decrypted raw data.</returns>
        public byte[] Decrypt(byte[] encryptedBytes, byte[] encryptionKey)
        {
            if (encryptedBytes == null)
            {
                throw new ArgumentNullException(nameof(encryptedBytes), "Encrypted data cannot be null.");
            }

            return ApiServiceOptions.PayloadEncryptor.Decrypt(encryptedBytes, encryptionKey);
        }
    }

}
