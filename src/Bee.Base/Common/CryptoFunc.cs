using System;
using System.Security.Cryptography;
using System.Text;

namespace Bee.Base
{
    /// <summary>
    /// 提供常用的密碼學功能，包括對稱加解密、雜湊與金鑰導出等方法。
    /// </summary>
    public static class CryptoFunc
    {
        /// <summary>
        /// AES 256 加密。
        /// </summary>
        /// <param name="bytes">未加密的 Byte 陣列。</param>
        /// <param name="key">加密金鑰，要求 32*8=256 位元，長度需為 32 碼。</param>
        /// <param name="iv">初始化向量，要求 6*8=128 位元，長度需為 16 碼。</param>
        public static byte[] AesEncrypt(byte[] bytes, string key, string iv)
        {
            if (StrFunc.IsEmpty(key))
                throw new ArgumentNullException("key");
            if (StrFunc.IsEmpty(iv))
                throw new ArgumentNullException("iv");

            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(iv);
            var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// AES 256 解密。
        /// </summary>
        /// <param name="bytes">已加密的 Byte 陣列。</param>
        /// <param name="key">加密金鑰，要求 32*8=256 位元，長度需為 32 碼。</param>
        /// <param name="iv">初始化向量，要求 6*8=128 位元，長度需為 16 碼。</param>
        public static byte[] AesDecrypt(byte[] bytes, string key, string iv)
        {
            if (StrFunc.IsEmpty(key))
                throw new ArgumentNullException("key");
            if (StrFunc.IsEmpty(iv))
                throw new ArgumentNullException("iv");

            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(iv);
            var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// AES 256 加密，隨機 Key 及 IV。
        /// </summary>
        /// <param name="bytes">未加密的 Byte 陣列。</param>
        public static byte[] AesEncrypt(byte[] bytes)
        {
            // 隨機產生 Key 及 IV
            string key = StrFunc.Left(Guid.NewGuid().ToString().Replace("-", ""), 32);
            string iv = StrFunc.Left(Guid.NewGuid().ToString().Replace("-", ""), 16);
            // 執行 AES 256 加密
            byte[] aesBytes = AesEncrypt(bytes, key, iv);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key + iv);
            // 合併資料
            byte[] resultBytes = new byte[keyBytes.Length + aesBytes.Length];
            Buffer.BlockCopy(keyBytes, 0, resultBytes, 0, keyBytes.Length);
            Buffer.BlockCopy(aesBytes, 0, resultBytes, keyBytes.Length, aesBytes.Length);
            return resultBytes;
        }

        /// <summary>
        /// AES 256 解密，隨機 Key 及 IV。
        /// </summary>
        /// <param name="bytes">已加密的 Byte 陣列。</param>
        public static byte[] AesDecrypt(byte[] bytes)
        {
            // 拆解資料
            byte[] keyBytes = new byte[32 + 16];
            byte[] aesBytes = new byte[bytes.Length - keyBytes.Length];
            Buffer.BlockCopy(bytes, 0, keyBytes, 0, keyBytes.Length);
            Buffer.BlockCopy(bytes, keyBytes.Length, aesBytes, 0, aesBytes.Length);
            // 取得 Key 及 IV
            string value = Encoding.UTF8.GetString(keyBytes);
            string key = StrFunc.Left(value, 32);
            string iV = StrFunc.Right(value, 16);
            // 執行 AES 256 解密
            byte[] resultBytes = AesDecrypt(aesBytes, key, iV);
            return resultBytes;
        }

        /// <summary>
        /// AES 256 加密，隨機 Key 及 IV。
        /// </summary>
        /// <param name="value">未加密字串。</param>
        public static string AesEncrypt(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            bytes = AesEncrypt(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// AES 256 解密，隨機 Key 及 IV。
        /// </summary>
        /// <param name="value">已加密的字串。</param>
        public static string AesDecrypt(string value)
        {
            byte[]  bytes = Convert.FromBase64String(value);
            bytes = AesDecrypt(bytes);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// AES 256 解密，隨機 Key 及 IV，若嘗試解密失敗，則傳回原字串。
        /// </summary>
        /// <param name="value">已加密的字串。</param>
        public static string AesTryDecrypt(string value)
        {
            try
            {
                return AesDecrypt(value);
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        /// SHA 512 不可逆加密，常用於密碼儲存。
        /// </summary>
        /// <param name="value">原始字串。</param>
        public static string Sha512Encrypt(string value)
        {
            SHA512 oSHA;
            byte[] oBytes;
            byte[] oResultBytes;

            oBytes = Encoding.UTF8.GetBytes(value);
            oSHA = new SHA512Managed();
            oResultBytes = oSHA.ComputeHash(oBytes);
            return Convert.ToBase64String(oResultBytes);
        }

        /// <summary>
        /// SHA 256 不可逆加密，常用於密碼儲存。
        /// </summary>
        /// <param name="value">原始字串。</param>
        public static string Sha256Encrypt(string value)
        {
            SHA256 oSHA;
            byte[] oBytes;
            byte[] oResultBytes;

            oBytes = Encoding.UTF8.GetBytes(value);
            oSHA = new SHA256Managed();
            oResultBytes = oSHA.ComputeHash(oBytes);
            return Convert.ToBase64String(oResultBytes);
        }
    }
}
