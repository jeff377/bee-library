using System.Security.Cryptography;
using System.Text;
using Bee.Base;
using Bee.Base.Security;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Encrypts and decrypts <see cref="DatabaseServer.Password"/> and
    /// <see cref="DatabaseItem.Password"/> fields on a <see cref="DatabaseSettings"/>
    /// instance in place. Idempotent: passwords already prefixed with <c>enc:</c>
    /// are skipped on encrypt; passwords without the prefix are left untouched on decrypt.
    /// </summary>
    /// <remarks>
    /// Phase 5 split the crypto concern out of <see cref="DatabaseSettings"/>'s
    /// <c>IObjectSerializeProcess</c> hooks so the DTO stops reaching to
    /// process-wide static state. Callers (<c>CacheDefineAccess</c> at read/save time)
    /// inject the combined key explicitly.
    /// </remarks>
    public static class DatabaseSettingsCryptor
    {
        private const string EncPrefix = "enc:";

        /// <summary>
        /// Encrypts every plain-text <c>Password</c> in place. No-op when
        /// <paramref name="combinedKey"/> is null or empty.
        /// </summary>
        /// <param name="settings">The database settings to mutate.</param>
        /// <param name="combinedKey">The 64-byte combined AES + HMAC key.</param>
        public static void EncryptInPlace(DatabaseSettings settings, byte[]? combinedKey)
        {
            ArgumentNullException.ThrowIfNull(settings);
            if (combinedKey == null || combinedKey.Length == 0) return;

            AesCbcHmacKeyGenerator.FromCombinedKey(combinedKey, out var aesKey, out var hmacKey);

            foreach (var server in settings.Servers!.Where(s => StringUtilities.IsNotEmpty(s.Password) && !s.Password.StartsWith(EncPrefix)))
            {
                server.Password = Encrypt(server.Password, aesKey, hmacKey);
            }

            foreach (var item in settings.Items!.Where(i => StringUtilities.IsNotEmpty(i.Password) && !i.Password.StartsWith(EncPrefix)))
            {
                item.Password = Encrypt(item.Password, aesKey, hmacKey);
            }
        }

        /// <summary>
        /// Decrypts every <c>enc:</c>-prefixed <c>Password</c> in place. No-op when
        /// <paramref name="combinedKey"/> is null or empty. Plain-text passwords are
        /// left unchanged; malformed ciphertext is replaced with the empty string.
        /// </summary>
        /// <param name="settings">The database settings to mutate.</param>
        /// <param name="combinedKey">The 64-byte combined AES + HMAC key.</param>
        public static void DecryptInPlace(DatabaseSettings settings, byte[]? combinedKey)
        {
            ArgumentNullException.ThrowIfNull(settings);
            if (combinedKey == null || combinedKey.Length == 0) return;

            AesCbcHmacKeyGenerator.FromCombinedKey(combinedKey, out var aesKey, out var hmacKey);

            foreach (var server in settings.Servers!)
                server.Password = Decrypt(server.Password, aesKey, hmacKey);

            foreach (var item in settings.Items!)
                item.Password = Decrypt(item.Password, aesKey, hmacKey);
        }

        private static string Encrypt(string plain, byte[] aesKey, byte[] hmacKey)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
            byte[] encrypted = AesCbcHmacCryptor.Encrypt(plainBytes, aesKey, hmacKey);
            return EncPrefix + Convert.ToBase64String(encrypted);
        }

        private static string Decrypt(string password, byte[] aesKey, byte[] hmacKey)
        {
            if (StringUtilities.IsEmpty(password) || !password.StartsWith(EncPrefix))
                return password;

            try
            {
                string base64 = password.Substring(EncPrefix.Length);
                byte[] encrypted = Convert.FromBase64String(base64);
                byte[] plain = AesCbcHmacCryptor.Decrypt(encrypted, aesKey, hmacKey);
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException)
            {
                // A malformed or tampered ciphertext (bad base64, HMAC mismatch, invalid length)
                // decrypts to nothing — fail closed. AesCbcHmacCryptor raises CryptographicException
                // on every integrity failure, so unexpected exceptions still propagate.
                return string.Empty;
            }
        }
    }
}
