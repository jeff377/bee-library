using System;
using System.IO;
using System.Security.Cryptography;

namespace Bee.Base.Security
{
    /// <summary>
    /// Provides file hash computation and verification functionality.
    /// </summary>
    public static class FileHashValidator
    {
        /// <summary>
        /// Verifies whether the SHA-256 hash of the specified file matches the expected value.
        /// </summary>
        /// <param name="filePath">The full path of the file to verify.</param>
        /// <param name="expectedSha256Hex">The expected SHA-256 hash as a hexadecimal string (case-insensitive).</param>
        /// <returns>True if the hashes match; otherwise, false.</returns>
        public static bool VerifySha256(string filePath, string expectedSha256Hex)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                var actualHex = BitConverter.ToString(hashBytes).Replace("-", "");
                return string.Equals(actualHex, expectedSha256Hex, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Computes the SHA-256 hash of the specified file and returns it as a hexadecimal string.
        /// </summary>
        /// <param name="filePath">The full path of the file.</param>
        /// <returns>The SHA-256 hash of the file as a hexadecimal string.</returns>
        public static string ComputeSha256(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
        }
    }
}
