using System.Buffers.Text;
using System.Security.Cryptography;

namespace Bee.Base.Security
{
    /// <summary>
    /// Composes, parses and validates the two-segment plaintext API key
    /// <c>{sysId}.{secret}</c> — for example <c>northwind-desktop.xLq9Kv2mNp8wR4tZ...</c>.
    /// </summary>
    /// <remarks>
    /// Splitting identity from verification is what lets the store hold only a hash while lookup
    /// stays O(1): the leading segment identifies the row, the trailing segment is checked against
    /// that row's hash. It is the shape used by GitHub personal access tokens and Stripe keys.
    /// <para>
    /// NOTE: the leading segment is not secret. It is chosen by whoever issues the key so logs and
    /// audit records name the calling application directly, and it can be enumerated without
    /// consequence — an attacker still needs the 256-bit secret.
    /// </para>
    /// </remarks>
    public static class ApiKeyFormat
    {
        /// <summary>Separator between the identifier and secret segments.</summary>
        public const char Separator = '.';

        /// <summary>Minimum length of the identifier segment.</summary>
        public const int MinSysIdLength = 3;

        /// <summary>Maximum length of the identifier segment.</summary>
        public const int MaxSysIdLength = 50;

        private const int SecretByteLength = 32; // 256-bit

        /// <summary>
        /// Generates a new 256-bit secret segment, encoded URL-safe so a key survives being carried
        /// in headers, query strings and configuration files unescaped.
        /// </summary>
        public static string CreateSecret()
        {
            return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SecretByteLength));
        }

        /// <summary>
        /// Composes the plaintext key from its two segments.
        /// </summary>
        /// <param name="sysId">The key identifier.</param>
        /// <param name="secret">The secret segment.</param>
        public static string Compose(string sysId, string secret)
        {
            return sysId + Separator + secret;
        }

        /// <summary>
        /// Returns whether the identifier segment is well formed: <see cref="MinSysIdLength"/> to
        /// <see cref="MaxSysIdLength"/> characters of lowercase letters, digits and hyphens, not
        /// starting or ending with a hyphen.
        /// </summary>
        /// <param name="sysId">The identifier to check.</param>
        /// <remarks>
        /// WARNING: the character set must never admit <see cref="Separator"/>. A dot inside the
        /// identifier would make the split ambiguous, so a key could be parsed into the wrong
        /// segments. Validation happens when a key is issued, not when one is verified, so this
        /// costs nothing on the request path.
        /// </remarks>
        public static bool IsValidSysId(string? sysId)
        {
            if (string.IsNullOrEmpty(sysId))
                return false;
            if (sysId.Length < MinSysIdLength || sysId.Length > MaxSysIdLength)
                return false;
            if (sysId[0] == '-' || sysId[^1] == '-')
                return false;

            foreach (char c in sysId)
            {
                bool allowed = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-';
                if (!allowed)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Splits a plaintext key into its identifier and secret segments.
        /// </summary>
        /// <param name="key">The raw key value supplied by the caller.</param>
        /// <param name="sysId">When this method returns <c>true</c>, the identifier segment.</param>
        /// <param name="secret">When this method returns <c>true</c>, the secret segment.</param>
        /// <returns><c>true</c> when the value is well formed; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Rejecting a malformed value here is the first line of defence against key scanning: a
        /// value that never parses costs one string split and never reaches the database.
        /// </remarks>
        public static bool TryParse(string? key, out string sysId, out string secret)
        {
            sysId = string.Empty;
            secret = string.Empty;
            if (string.IsNullOrWhiteSpace(key))
                return false;

            int index = key.IndexOf(Separator);
            if (index <= 0 || index == key.Length - 1)
                return false;

            string candidateId = key.Substring(0, index);
            if (!IsValidSysId(candidateId))
                return false;

            sysId = candidateId;
            secret = key.Substring(index + 1);
            return true;
        }
    }
}
