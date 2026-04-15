using System;
using System.IO;
using System.Security.Cryptography;

namespace Bee.Base.Security
{
    /// <summary>
    /// Encryption and decryption utility using AES-CBC and HMAC authentication.
    /// Uses a 256-bit AES key and a 256-bit HMAC-SHA256 key;
    /// every encryption uses a random IV and appends an integrity verification code (HMAC).
    /// </summary>
    public static class AesCbcHmacCryptor
    {
        /// <summary>
        /// Encrypts data using AES-CBC and appends an HMAC authentication code.
        /// </summary>
        /// <param name="plainBytes">The byte array of the original data.</param>
        /// <param name="aesKey">The AES symmetric encryption key (32 bytes).</param>
        /// <param name="hmacKey">The HMAC verification key (32 bytes).</param>
        /// <returns>The encrypted byte data, containing the IV, ciphertext, and HMAC.</returns>
        public static byte[] Encrypt(byte[] plainBytes, byte[] aesKey, byte[] hmacKey)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    using (var ms = new MemoryStream())
                    using (var writer = new BinaryWriter(ms))
                    {
                        writer.Write(iv.Length);
                        writer.Write(iv);
                        writer.Write(cipherBytes.Length);
                        writer.Write(cipherBytes);

                        byte[] data = ms.ToArray();

                        using (var hmac = new HMACSHA256(hmacKey))
                        {
                            byte[] hmacBytes = hmac.ComputeHash(data);
                            return Combine(data, hmacBytes);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts AES-CBC encrypted data and verifies the HMAC.
        /// </summary>
        /// <param name="encryptedData">The encrypted data.</param>
        /// <param name="aesKey">The AES symmetric encryption key (32 bytes).</param>
        /// <param name="hmacKey">The HMAC verification key (32 bytes).</param>
        /// <returns>The decrypted original data.</returns>
        /// <exception cref="CryptographicException">Thrown when HMAC validation fails or the data format is invalid.</exception>
        public static byte[] Decrypt(byte[] encryptedData, byte[] aesKey, byte[] hmacKey)
        {
            // Minimum: 4 (ivLength) + 16 (IV) + 4 (cipherLength) + 16 (min ciphertext) + 32 (HMAC) = 72
            if (encryptedData == null || encryptedData.Length < 72)
                throw new CryptographicException("Invalid encrypted data.");

            using (var ms = new MemoryStream(encryptedData))
            using (var reader = new BinaryReader(ms))
            {
                int ivLength = reader.ReadInt32();
                if (ivLength < 16 || ivLength > 32)
                    throw new CryptographicException("Invalid IV length.");

                byte[] iv = reader.ReadBytes(ivLength);

                int cipherLength = reader.ReadInt32();
                // Remaining bytes after ivLength field (4) + IV + cipherLength field (4) must hold ciphertext + HMAC (32)
                if (cipherLength <= 0 || cipherLength > encryptedData.Length - ivLength - 40)
                    throw new CryptographicException("Invalid cipher data length.");

                byte[] cipherBytes = reader.ReadBytes(cipherLength);
                byte[] hmacBytes = reader.ReadBytes(32); // SHA-256 length

                byte[] dataToVerify = new byte[ivLength + cipherLength + 8];
                Array.Copy(encryptedData, 0, dataToVerify, 0, dataToVerify.Length);

                using (var hmac = new HMACSHA256(hmacKey))
                {
                    byte[] computedHmac = hmac.ComputeHash(dataToVerify);
                    if (!CompareBytes(hmacBytes, computedHmac))
                        throw new CryptographicException("HMAC validation failed.");
                }

                using (var aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Combines two byte arrays into one.
        /// </summary>
        private static byte[] Combine(byte[] a, byte[] b)
        {
            byte[] result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }

        /// <summary>
        /// Compares two byte arrays using constant-time comparison to prevent timing attacks.
        /// </summary>
        private static bool CompareBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];
            return result == 0;
        }
    }
}
