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
                var actualBytes = sha256.ComputeHash(stream);
                var expectedBytes = HexToBytes(expectedSha256Hex);
                if (expectedBytes == null || expectedBytes.Length != actualBytes.Length)
                    return false;
                return FixedTimeEquals(actualBytes, expectedBytes);
            }
        }

        /// <summary>
        /// Converts a hexadecimal string to a byte array. Returns null if the input is invalid.
        /// </summary>
        private static byte[]? HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
                return null;
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                try { bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16); }
                catch { return null; }
            }
            return bytes;
        }

        /// <summary>
        /// Performs a constant-time byte array comparison to prevent timing side-channel attacks.
        /// </summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];
            return result == 0;
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
                return Convert.ToHexString(hashBytes);
            }
        }
    }
}
