using System.Security.Cryptography;
using System.Text;

namespace Bee.Base.Security
{
    /// <summary>
    /// Static utility class providing RSA encryption, decryption, key generation, and export functionality.
    /// </summary>
    /// <remarks>
    /// Keys are exchanged as PEM strings: public key in SPKI format (<c>-----BEGIN PUBLIC KEY-----</c>),
    /// private key in PKCS#1 format (<c>-----BEGIN RSA PRIVATE KEY-----</c>). PEM is supported across all
    /// .NET runtimes including Blazor WebAssembly, unlike the legacy XML key format which is Windows-only.
    /// Suitable for asymmetric encryption scenarios such as login credentials and session key exchange.
    /// Uses OAEP-SHA256 padding to prevent padding oracle attacks.
    /// </remarks>
    public static class RsaCryptor
    {
        /// <summary>
        /// Generates an RSA key pair in PEM format.
        /// </summary>
        /// <param name="publicKey">The output public key in SPKI PEM format.</param>
        /// <param name="privateKey">The output private key in PKCS#1 PEM format.</param>
        public static void GenerateRsaKeyPair(out string publicKey, out string privateKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.KeySize = 2048;
                publicKey = rsa.ExportSubjectPublicKeyInfoPem();
                privateKey = rsa.ExportRSAPrivateKeyPem();
            }
        }

        /// <summary>
        /// Encrypts data using an RSA public key and returns the result as a Base64 string.
        /// </summary>
        /// <param name="plainText">The plaintext string to encrypt.</param>
        /// <param name="publicKey">The RSA public key in PEM format (public parameters only).</param>
        /// <returns>The encrypted data as a Base64-encoded string.</returns>
        public static string EncryptWithPublicKey(string plainText, string publicKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(publicKey);
                var data = Encoding.UTF8.GetBytes(plainText);
                var encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
                return Convert.ToBase64String(encrypted);
            }
        }

        /// <summary>
        /// Decrypts Base64-encoded data using an RSA private key.
        /// </summary>
        /// <param name="base64CipherText">The ciphertext to decrypt, in Base64-encoded format.</param>
        /// <param name="privateKey">The RSA private key in PEM format (includes both public and private parameters).</param>
        /// <returns>The decrypted plaintext string.</returns>
        public static string DecryptWithPrivateKey(string base64CipherText, string privateKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(privateKey);
                var data = Convert.FromBase64String(base64CipherText);
                var decrypted = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }
}
