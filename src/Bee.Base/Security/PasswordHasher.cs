using System;
using System.Security.Cryptography;

namespace Bee.Base.Security
{
    /// <summary>
    /// Utility class for password hashing and verification using the PBKDF2 algorithm.
    /// </summary>
    public class PasswordHasher
    {
        private const int SaltSize = 16; // 128-bit
        private const int HashSize = 32; // 256-bit
        private const int Iterations = 100000;

        // Version prefix for SHA-256 hashes. Legacy hashes without this prefix use SHA-1 (backwards compatible).
        private const string V2Prefix = "v2.";

        /// <summary>
        /// Creates a hashed password string.
        /// New format (v2): v2.{iterations}.{saltBase64}.{hashBase64} — uses PBKDF2-SHA256.
        /// </summary>
        /// <param name="password">The original password.</param>
        /// <returns>The hashed password string.</returns>
        public string HashPassword(string password)
        {
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);
            var hash = PBKDF2SHA256(password, salt, Iterations, HashSize);
            return $"{V2Prefix}{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies whether the provided password matches the stored hash.
        /// Supports both v2 (SHA-256) and legacy (SHA-1) formats for backwards compatibility.
        /// </summary>
        /// <param name="password">The password entered by the user.</param>
        /// <param name="hashedPassword">The stored hashed password string.</param>
        /// <returns>True if the password matches; otherwise, false.</returns>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                if (hashedPassword.StartsWith(V2Prefix, StringComparison.Ordinal))
                {
                    // v2 format: v2.{iterations}.{salt}.{hash} — PBKDF2-SHA256
                    var inner = hashedPassword.Substring(V2Prefix.Length);
                    var parts = inner.Split('.');
                    if (parts.Length != 3) return false;
                    int iterations = int.Parse(parts[0]);
                    byte[] salt = Convert.FromBase64String(parts[1]);
                    byte[] storedHash = Convert.FromBase64String(parts[2]);
                    var computedHash = PBKDF2SHA256(password, salt, iterations, storedHash.Length);
                    return FixedTimeEquals(storedHash, computedHash);
                }
                else
                {
                    // Legacy format: {iterations}.{salt}.{hash} — PBKDF2-SHA1 (read-only, for existing passwords)
                    var parts = hashedPassword.Split('.');
                    if (parts.Length != 3) return false;
                    int iterations = int.Parse(parts[0]);
                    byte[] salt = Convert.FromBase64String(parts[1]);
                    byte[] storedHash = Convert.FromBase64String(parts[2]);
                    var computedHash = PBKDF2SHA1Legacy(password, salt, iterations, storedHash.Length);
                    return FixedTimeEquals(storedHash, computedHash);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a PBKDF2-SHA256 hash.
        /// </summary>
        private static byte[] PBKDF2SHA256(string password, byte[] salt, int iterations, int outputBytes)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                System.Text.Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, outputBytes);
        }

        /// <summary>
        /// Generates a PBKDF2-SHA1 hash for verifying legacy passwords only. Do not use for new hashes.
        /// SHA1 is intentionally used here to match existing stored hashes; cannot be upgraded without
        /// invalidating all legacy passwords. New passwords always use PBKDF2-SHA256 via HashPassword().
        /// </summary>
        private static byte[] PBKDF2SHA1Legacy(string password, byte[] salt, int iterations, int outputBytes)
        {
#pragma warning disable SYSLIB0041
#pragma warning disable SYSLIB0060
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations)) // NOSONAR: legacy SHA1 required for backwards compatibility
            {
                return pbkdf2.GetBytes(outputBytes);
            }
#pragma warning restore SYSLIB0060
#pragma warning restore SYSLIB0041
        }

        /// <summary>
        /// Performs a constant-time comparison to prevent timing attacks.
        /// </summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }
    }


}
