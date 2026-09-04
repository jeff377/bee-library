using System.Globalization;
using System.Security.Cryptography;

namespace Bee.Base.Security
{
    /// <summary>
    /// Utility class for password hashing and verification using the PBKDF2 algorithm.
    /// </summary>
    public static class PasswordHasher
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
        public static string HashPassword(string password)
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
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                if (hashedPassword.StartsWith(V2Prefix, StringComparison.Ordinal))
                {
                    // v2 format: v2.{iterations}.{salt}.{hash} — PBKDF2-SHA256
                    var inner = hashedPassword.Substring(V2Prefix.Length);
                    var parts = inner.Split('.');
                    if (parts.Length != 3) return false;
                    int iterations = int.Parse(parts[0], CultureInfo.InvariantCulture);
                    byte[] salt = Convert.FromBase64String(parts[1]);
                    byte[] storedHash = Convert.FromBase64String(parts[2]);
                    if (!IsUsableStoredHash(salt, storedHash)) { return false; }
                    var computedHash = PBKDF2SHA256(password, salt, iterations, storedHash.Length);
                    return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
                }
                else
                {
                    // Legacy format: {iterations}.{salt}.{hash} — PBKDF2-SHA1 (read-only, for existing passwords)
                    var parts = hashedPassword.Split('.');
                    if (parts.Length != 3) return false;
                    int iterations = int.Parse(parts[0], CultureInfo.InvariantCulture);
                    byte[] salt = Convert.FromBase64String(parts[1]);
                    byte[] storedHash = Convert.FromBase64String(parts[2]);
                    if (!IsUsableStoredHash(salt, storedHash)) { return false; }
                    var computedHash = PBKDF2SHA1Legacy(password, salt, iterations, storedHash.Length);
                    return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
                }
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
            {
                // A malformed stored hash (bad base64, unparsable iteration count, invalid
                // PBKDF2 parameters) means the password cannot match — fail closed. Unexpected
                // exceptions are left to propagate rather than masquerading as a wrong password.
                return false;
            }
        }

        /// <summary>
        /// Determines whether the components parsed out of a stored hash can represent a real hash.
        /// </summary>
        /// <param name="salt">The salt parsed from the stored value.</param>
        /// <param name="storedHash">The hash parsed from the stored value.</param>
        /// <remarks>
        /// WARNING: without this an empty hash segment authenticates every password. PBKDF2 asked for
        /// zero output bytes returns an empty array, and
        /// <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        /// reports two empty spans as equal — so a stored value of <c>v2.100000..</c> would verify
        /// against anything. Both format branches parse their components the same way and so need the
        /// same guard.
        /// <para>
        /// The iteration count is deliberately not floored here. A low count weakens offline cracking
        /// of that one password, but reaching it already requires the ability to write the stored
        /// value, and a floor would lock out legitimate legacy hashes created with fewer iterations.
        /// Counts of zero or less are already rejected: PBKDF2 throws for them and the caller's
        /// catch turns that into a failed verification.
        /// </para>
        /// </remarks>
        private static bool IsUsableStoredHash(byte[] salt, byte[] storedHash)
            => salt.Length > 0 && storedHash.Length > 0;

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
            // NOSONAR: legacy SHA1 required for backwards compatibility with existing stored hashes.
            return Rfc2898DeriveBytes.Pbkdf2(
                System.Text.Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA1, outputBytes);
        }
    }


}
