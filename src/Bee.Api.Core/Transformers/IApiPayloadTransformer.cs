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
        /// Serializes and compresses the specified object using an explicitly chosen serializer.
        /// </summary>
        /// <param name="payload">The raw data object to process.</param>
        /// <param name="type">The type of the object.</param>
        /// <param name="serializer">The body codec to serialize with.</param>
        /// <returns>The processed data (typically a byte array).</returns>
        /// <remarks>
        /// Only called when a payload names a codec other than the deployment default, so a
        /// transformer written before negotiation existed keeps serving every call that names
        /// none. The default implementation refuses rather than quietly falling back to
        /// <see cref="ApiServiceOptions.PayloadSerializer"/>: falling back would encode the body
        /// with a codec the caller did not ask for, and the caller would decode the answer as the
        /// one it did.
        /// </remarks>
        /// <exception cref="NotSupportedException">The transformer does not implement codec selection.</exception>
        byte[] Encode(object payload, Type type, IApiPayloadSerializer serializer)
            => throw new NotSupportedException(
                $"{GetType().FullName} does not support per-request codec selection. Implement the three-argument Encode overload to serve clients that negotiate one.");

        /// <summary>
        /// Decompresses and deserializes the processed data using an explicitly chosen serializer.
        /// </summary>
        /// <param name="payload">The processed data (typically a byte array).</param>
        /// <param name="type">The target object type.</param>
        /// <param name="serializer">The body codec to deserialize with.</param>
        /// <returns>The restored original data object.</returns>
        /// <remarks>See <see cref="Encode(object, Type, IApiPayloadSerializer)"/>.</remarks>
        /// <exception cref="NotSupportedException">The transformer does not implement codec selection.</exception>
        object? Decode(object payload, Type type, IApiPayloadSerializer serializer)
            => throw new NotSupportedException(
                $"{GetType().FullName} does not support per-request codec selection. Implement the three-argument Decode overload to serve clients that negotiate one.");

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
