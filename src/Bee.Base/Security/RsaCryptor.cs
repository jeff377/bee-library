using System;
using System.Security.Cryptography;
using System.Text;

namespace Bee.Base
{
    /// <summary>
    /// 提供 RSA 加密與解密、金鑰產生與匯出功能的靜態工具類別。
    /// </summary>
    /// <remarks>
    /// 支援以 RSA 公私鑰進行字串加解密，並可匯出/匯入 XML 格式的金鑰資料。
    /// 適用於登入憑證、Session 金鑰交換等非對稱加密應用情境。
    /// </remarks>
    public static class RsaCryptor
    {
        /// <summary>
        /// 產生 RSA 對稱金鑰（XML 格式）
        /// </summary>
        /// <param name="publicKeyXml">輸出公鑰 XML</param>
        /// <param name="privateKeyXml">輸出私鑰 XML</param>
        public static void GenerateRsaKeyPair(out string publicKeyXml, out string privateKeyXml)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                publicKeyXml = rsa.ToXmlString(false); // 公鑰
                privateKeyXml = rsa.ToXmlString(true); // 私鑰
            }
        }

        /// <summary>
        /// 使用 RSA 公鑰加密資料（Base64）
        /// </summary>
        /// <param name="plainText">要加密的明文字串。</param>
        /// <param name="publicKeyXml">RSA 公鑰（XML 格式，僅含公開參數）。</param>
        /// <returns>加密後的資料，為 Base64 編碼字串。</returns>
        public static string EncryptWithPublicKey(string plainText, string publicKeyXml)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.FromXmlString(publicKeyXml);
                var data = Encoding.UTF8.GetBytes(plainText);
                var encrypted = rsa.Encrypt(data, false);
                return Convert.ToBase64String(encrypted);
            }
        }

        /// <summary>
        /// 使用 RSA 私鑰解密資料（Base64）
        /// </summary>
        /// <param name="base64CipherText">要解密的密文，為 Base64 編碼格式。</param>
        /// <param name="privateKeyXml">RSA 私鑰（XML 格式，包含公開與私密參數）。</param>
        /// <returns>解密後的明文字串。</returns>
        public static string DecryptWithPrivateKey(string base64CipherText, string privateKeyXml)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.FromXmlString(privateKeyXml);
                var data = Convert.FromBase64String(base64CipherText);
                var decrypted = rsa.Decrypt(data, false);
                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }
}

