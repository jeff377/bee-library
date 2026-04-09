using System;
using System.Security.Cryptography;
using System.Text;

namespace Bee.Base.Security
{
    /// <summary>
    /// Static utility class providing RSA encryption, decryption, key generation, and export functionality.
    /// </summary>
    /// <remarks>
    /// Supports string encryption and decryption using RSA public/private keys, with XML key export/import.
    /// Suitable for asymmetric encryption scenarios such as login credentials and session key exchange.
    /// Uses OAEP-SHA256 padding to prevent padding oracle attacks.
    /// </remarks>
    public static class RsaCryptor
    {
        /// <summary>
        /// Generates an RSA key pair in XML format.
        /// </summary>
        /// <param name="publicKeyXml">The output public key XML.</param>
        /// <param name="privateKeyXml">The output private key XML.</param>
        public static void GenerateRsaKeyPair(out string publicKeyXml, out string privateKeyXml)
        {
            using (var rsa = RSA.Create())
            {
                rsa.KeySize = 2048;
                publicKeyXml = rsa.ToXmlString(false); // Public key
                privateKeyXml = rsa.ToXmlString(true); // Private key
            }
        }

        /// <summary>
        /// Encrypts data using an RSA public key and returns the result as a Base64 string.
        /// </summary>
        /// <param name="plainText">The plaintext string to encrypt.</param>
        /// <param name="publicKeyXml">The RSA public key in XML format (public parameters only).</param>
        /// <returns>The encrypted data as a Base64-encoded string.</returns>
        public static string EncryptWithPublicKey(string plainText, string publicKeyXml)
        {
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(publicKeyXml);
                var data = Encoding.UTF8.GetBytes(plainText);
                var encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
                return Convert.ToBase64String(encrypted);
            }
        }

        /// <summary>
        /// Decrypts Base64-encoded data using an RSA private key.
        /// </summary>
        /// <param name="base64CipherText">The ciphertext to decrypt, in Base64-encoded format.</param>
        /// <param name="privateKeyXml">The RSA private key in XML format (includes both public and private parameters).</param>
        /// <returns>The decrypted plaintext string.</returns>
        public static string DecryptWithPrivateKey(string base64CipherText, string privateKeyXml)
        {
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(privateKeyXml);
                var data = Convert.FromBase64String(base64CipherText);
                var decrypted = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }
}
