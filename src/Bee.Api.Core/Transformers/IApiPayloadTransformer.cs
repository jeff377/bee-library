namespace Bee.Api.Core.Transformers
{
    /// <summary>
    /// Defines the interface for the API payload transformer, providing data serialization, compression, and encryption/decryption.
    /// </summary>
    public interface IApiPayloadTransformer
    {
        /// <summary>
        /// Serializes and compresses the specified object.
        /// </summary>
        /// <param name="payload">The raw data object to process.</param>
        /// <param name="type">The type of the object.</param>
        /// <returns>The processed data (typically a byte array).</returns>
        byte[] Encode(object payload, Type type);

        /// <summary>
        /// Decompresses and deserializes the processed data back to the original object.
        /// </summary>
        /// <param name="payload">The processed data (typically a byte array).</param>
        /// <param name="type">The target object type.</param>
        /// <returns>The restored original data object.</returns>
        object? Decode(object payload, Type type);

        /// <summary>
        /// Encrypts the specified byte data only.
        /// </summary>
        /// <param name="rawBytes">The raw byte data.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <returns>The encrypted data.</returns>
        byte[] Encrypt(byte[] rawBytes, byte[] encryptionKey);

        /// <summary>
        /// Decrypts the specified byte data only.
        /// </summary>
        /// <param name="encryptedBytes">The encrypted data.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <returns>The decrypted raw data.</returns>
        byte[] Decrypt(byte[] encryptedBytes, byte[] encryptionKey);
    }
}
