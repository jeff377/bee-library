using System;
using System.IO;
using System.Security.Cryptography;

namespace Bee.Base
{
    /// <summary>
    /// AES-CBC 加密與 HMAC 驗證的加解密工具。
    /// 使用 256-bit AES 金鑰與 256-bit HMAC-SHA256 金鑰，
    /// 每次加密皆使用隨機 IV 並附帶完整性驗證碼（HMAC）。
    /// </summary>
    public static class AesCbcHmacCryptor
    {
        /// <summary>
        /// 進行 AES-CBC 加密，並附加 HMAC 驗證碼。
        /// </summary>
        /// <param name="plainBytes">原始資料的位元組陣列。</param>
        /// <param name="aesKey">AES 對稱加密金鑰（32 bytes）。</param>
        /// <param name="hmacKey">HMAC 驗證用金鑰（32 bytes）。</param>
        /// <returns>加密後的位元組資料，格式包含 IV、密文與 HMAC。</returns>
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
        /// 解密 AES-CBC 加密資料，並驗證 HMAC 是否一致。
        /// </summary>
        /// <param name="encryptedData">加密後的資料。</param>
        /// <param name="aesKey">AES 對稱加密金鑰（32 bytes）。</param>
        /// <param name="hmacKey">HMAC 驗證用金鑰（32 bytes）。</param>
        /// <returns>解密後的原始資料。</returns>
        /// <exception cref="CryptographicException">HMAC 驗證失敗或資料格式錯誤。</exception>
        public static byte[] Decrypt(byte[] encryptedData, byte[] aesKey, byte[] hmacKey)
        {
            using (var ms = new MemoryStream(encryptedData))
            using (var reader = new BinaryReader(ms))
            {
                int ivLength = reader.ReadInt32();
                byte[] iv = reader.ReadBytes(ivLength);
                int cipherLength = reader.ReadInt32();
                byte[] cipherBytes = reader.ReadBytes(cipherLength);
                byte[] hmacBytes = reader.ReadBytes(32); // SHA-256 長度

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
        /// 合併兩個位元組陣列。
        /// </summary>
        private static byte[] Combine(byte[] a, byte[] b)
        {
            byte[] result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }

        /// <summary>
        /// 安全比較兩組位元組內容是否相同。
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
