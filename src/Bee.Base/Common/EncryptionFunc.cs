using System;
using System.Security.Cryptography;
using System.Text;

namespace Bee.Base
{
    /// <summary>
    /// 加解密函式庫。
    /// </summary>
    public static class EncryptionFunc
    {
        /// <summary>
        /// AES 256 加密。
        /// </summary>
        /// <param name="bytes">未加密的 Byte 陣列。</param>
        /// <param name="key">加密金鑰，要求 32*8=256 位元，長度需為 32 碼。</param>
        /// <param name="iv">初始化向量，要求 6*8=128 位元，長度需為 16 碼。</param>
        public static byte[] AesEncrypt(byte[] bytes, string key, string iv)
        {
            Aes oAes;
            ICryptoTransform oTransform;

            if (StrFunc.IsEmpty(key))
                throw new ArgumentNullException("key");
            if (StrFunc.IsEmpty(iv))
                throw new ArgumentNullException("iv");

            oAes = Aes.Create();
            oAes.Mode = CipherMode.CBC;
            oAes.Padding = PaddingMode.PKCS7;
            oAes.Key = Encoding.UTF8.GetBytes(key);
            oAes.IV = Encoding.UTF8.GetBytes(iv);
            oTransform = oAes.CreateEncryptor();
            return oTransform.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// AES 256 解密。
        /// </summary>
        /// <param name="bytes">已加密的 Byte 陣列。</param>
        /// <param name="key">加密金鑰，要求 32*8=256 位元，長度需為 32 碼。</param>
        /// <param name="iv">初始化向量，要求 6*8=128 位元，長度需為 16 碼。</param>
        public static byte[] AesDecrypt(byte[] bytes, string key, string iv)
        {
            Aes oAes;
            ICryptoTransform oTransform;

            if (StrFunc.IsEmpty(key))
                throw new ArgumentNullException("key");
            if (StrFunc.IsEmpty(iv))
                throw new ArgumentNullException("iv");

            oAes = Aes.Create();
            oAes.Mode = CipherMode.CBC;
            oAes.Padding = PaddingMode.PKCS7;
            oAes.Key = Encoding.UTF8.GetBytes(key);
            oAes.IV = Encoding.UTF8.GetBytes(iv);
            oTransform = oAes.CreateDecryptor();
            return oTransform.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// AES 256 加密，隨機 Key 及 IV。
        /// </summary>
        /// <param name="bytes">未加密的 Byte 陣列。</param>
        public static byte[] AesEncrypt(byte[] bytes)
        {
            byte[] oAesBytes;
            byte[] oKeyBytes;
            byte[] oBytes;
            string sKey;
            string sIV;

            // 隨機產生 Key 及 IV
            sKey = StrFunc.Left(Guid.NewGuid().ToString().Replace("-", ""), 32);
            sIV = StrFunc.Left(Guid.NewGuid().ToString().Replace("-", ""), 16);
            // 執行 AES 256 加密
            oAesBytes = AesEncrypt(bytes, sKey, sIV);
            oKeyBytes = Encoding.UTF8.GetBytes(sKey + sIV);
            // 合併資料
            oBytes = new byte[oKeyBytes.Length + oAesBytes.Length];
            Buffer.BlockCopy(oKeyBytes, 0, oBytes, 0, oKeyBytes.Length);
            Buffer.BlockCopy(oAesBytes, 0, oBytes, oKeyBytes.Length, oAesBytes.Length);
            return oBytes;
        }

        /// <summary>
        /// AES 256 解密，隨機 Key 及 IV。
        /// </summary>
        /// <param name="bytes">已加密的 Byte 陣列。</param>
        public static byte[] AesDecrypt(byte[] bytes)
        {
            byte[] oAesBytes;
            byte[] oKeyBytes;
            byte[] oBytes;
            string sValue;
            string sKey;
            string sIV;

            // 拆解資料
            oKeyBytes = new byte[32 + 16];
            oAesBytes = new byte[bytes.Length - oKeyBytes.Length];
            Buffer.BlockCopy(bytes, 0, oKeyBytes, 0, oKeyBytes.Length);
            Buffer.BlockCopy(bytes, oKeyBytes.Length, oAesBytes, 0, oAesBytes.Length);
            // 取得 Key 及 IV
            sValue = Encoding.UTF8.GetString(oKeyBytes);
            sKey = StrFunc.Left(sValue, 32);
            sIV = StrFunc.Right(sValue, 16);
            // 執行 AES 256 解密
            oBytes = AesDecrypt(oAesBytes, sKey, sIV);
            return oBytes;
        }

        /// <summary>
        /// AES 256 加密，隨機 Key 及 IV。
        /// </summary>
        /// <param name="value">未加密字串。</param>
        public static string AesEncrypt(string value)
        {
            byte[] oBytes;

            oBytes = Encoding.UTF8.GetBytes(value);
            oBytes = AesEncrypt(oBytes);
            return  Convert.ToBase64String(oBytes);
        }

        /// <summary>
        /// AES 256 解密，隨機 Key 及 IV。
        /// </summary>
        /// <param name="value">已加密的字串。</param>
        public static string AesDecrypt(string value)
        {
            byte[] oBytes;

            oBytes = Convert.FromBase64String(value);
            oBytes = AesDecrypt(oBytes);
            return Encoding.UTF8.GetString(oBytes);
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
