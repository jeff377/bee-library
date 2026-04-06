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

        /// <summary>
        /// Creates a hashed password string in the format: {iterations}.{saltBase64}.{hashBase64}.
        /// </summary>
        /// <param name="password">The original password.</param>
        /// <returns>The hashed password string.</returns>
        public string HashPassword(string password)
        {
            var salt = new byte[SaltSize];
#pragma warning disable SYSLIB0023
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
#pragma warning restore SYSLIB0023

            var hash = PBKDF2(password, salt, Iterations, HashSize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies whether the provided password matches the stored hash.
        /// </summary>
        /// <param name="password">The password entered by the user.</param>
        /// <param name="hashedPassword">The stored hashed password string (format: {iterations}.{salt}.{hash}).</param>
        /// <returns>True if the password matches; otherwise, false.</returns>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                var parts = hashedPassword.Split('.');
                if (parts.Length != 3)
                    return false;

                int iterations = int.Parse(parts[0]);
                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] storedHash = Convert.FromBase64String(parts[2]);

                var computedHash = PBKDF2(password, salt, iterations, storedHash.Length);
                return FixedTimeEquals(storedHash, computedHash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a password hash using the PBKDF2 algorithm.
        /// </summary>
        private static byte[] PBKDF2(string password, byte[] salt, int iterations, int outputBytes)
        {
#pragma warning disable SYSLIB0041
#pragma warning disable SYSLIB0060
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
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
