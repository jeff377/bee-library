using System;
using System.Security.Cryptography;

namespace Bee.Base
{
    /// <summary>
    /// 使用 PBKDF2 演算法進行密碼雜湊與驗證的工具類別。
    /// </summary>
    public class TPasswordHasher
    {
        private const int SaltSize = 16; // 128-bit
        private const int HashSize = 32; // 256-bit
        private const int Iterations = 100000;

        /// <summary>
        /// 建立雜湊後的密碼字串（格式：{iterations}.{saltBase64}.{hashBase64}）。
        /// </summary>
        /// <param name="password">原始密碼</param>
        /// <returns>雜湊後的密碼字串</returns>
        public string HashPassword(string password)
        {
            var salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            var hash = PBKDF2(password, salt, Iterations, HashSize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// 驗證提供的密碼是否與儲存的雜湊值一致。
        /// </summary>
        /// <param name="password">用戶輸入的密碼</param>
        /// <param name="hashedPassword">儲存的雜湊密碼字串（格式：{iterations}.{salt}.{hash}）</param>
        /// <returns>是否驗證成功</returns>
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
        /// 使用 PBKDF2 演算法生成密碼雜湊值。
        /// </summary>
        private static byte[] PBKDF2(string password, byte[] salt, int iterations, int outputBytes)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return pbkdf2.GetBytes(outputBytes);
            }
        }

        /// <summary>
        /// 使用固定時間比較以防止時間攻擊。
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
