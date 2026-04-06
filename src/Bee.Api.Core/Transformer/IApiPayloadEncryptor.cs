namespace Bee.Api.Core.Transformer
{
    /// <summary>
    /// Interface for the API transport layer data encryption and decryption strategy.
    /// Provides byte data encryption and decryption to protect data security during transmission.
    /// </summary>
    public interface IApiPayloadEncryptor
    {
        /// <summary>
        /// Gets the identifier string for the encryption algorithm, e.g., "aes" or "rsa".
        /// </summary>
        string EncryptionMethod { get; }

        /// <summary>
        /// Encrypts the specified raw byte data.
        /// </summary>
        /// <param name="bytes">The raw byte data.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <returns>The encrypted byte data.</returns>
        byte[] Encrypt(byte[] bytes, byte[] encryptionKey);

        /// <summary>
        /// Decrypts the specified encrypted byte data back to its original form.
        /// </summary>
        /// <param name="bytes">The encrypted byte data.</param>
        /// <param name="encryptionKey">The encryption key.</param>
        /// <returns>The decrypted byte data.</returns>
        byte[] Decrypt(byte[] bytes, byte[] encryptionKey);
    }
}
