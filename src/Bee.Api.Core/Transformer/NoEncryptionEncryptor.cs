namespace Bee.Api.Core.Transformer
{
    /// <summary>
    /// An encryptor implementation that performs no encryption or decryption.
    /// </summary>
    public class NoEncryptionEncryptor : IApiPayloadEncryptor
    {
        /// <summary>
        /// Gets the identifier string for the encryption algorithm. "none" indicates no encryption is applied.
        /// </summary>
        public string EncryptionMethod => "none";

        /// <summary>
        /// Returns the original data unchanged; no encryption is performed.
        /// </summary>
        public byte[] Encrypt(byte[] bytes, byte[] encryptionKey)
        {
            return bytes;
        }

        /// <summary>
        /// Returns the original data unchanged; no decryption is performed.
        /// </summary>
        public byte[] Decrypt(byte[] bytes, byte[] encryptionKey)
        {
            return bytes;
        }
    }
}
