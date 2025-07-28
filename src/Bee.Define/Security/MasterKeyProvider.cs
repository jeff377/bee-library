using System;
using System.IO;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 主金鑰提供者，從設定來源載入主金鑰。
    /// </summary>
    public static class MasterKeyProvider
    {
        /// <summary>
        /// 取得主金鑰內容。
        /// </summary>
        /// <param name="source">主金鑰來源設定。</param>
        /// <param name="autoCreate">是否自動建立主金鑰。</param>
        /// <returns>解碼後的主金鑰位元組陣列。</returns>
        public static byte[] GetMasterKey(MasterKeySource source, bool autoCreate =false)
        {
            string keyText;

            switch (source.Type)
            {
                case MasterKeySourceType.File:
                    keyText = LoadFromFile(source.Value, autoCreate);
                    break;

                case MasterKeySourceType.Environment:
                    keyText = LoadFromEnvironment(source.Value, autoCreate);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported master key source type.");
            }

            if (string.IsNullOrWhiteSpace(keyText))
                throw new InvalidOperationException("Master key is empty or not found.");

            try
            {
                return Convert.FromBase64String(keyText.Trim());
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Master key is not valid Base64 format.", ex);
            }
        }

        /// <summary>
        /// 由檔案載入主金鑰內容。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        /// <param name="autoCreate">是否自動建立主金鑰。</param>
        /// <returns>主金鑰內容。</returns>
        private static string LoadFromFile(string filePath, bool autoCreate)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                filePath = "Master.key";
            }

            // 若為相對路徑，則補上 BackendInfo.DefinePath
            if (!Path.IsPathRooted(filePath))
            {
                filePath = FileFunc.PathCombine(BackendInfo.DefinePath, filePath);
            }

            if (!File.Exists(filePath))
            {
                if (autoCreate)
                {
                    string newKey = GenerateNewKey();
                    File.WriteAllText(filePath, newKey);
                    return newKey;
                }
                throw new FileNotFoundException("Master key file not found: " + filePath);
            }

            return File.ReadAllText(filePath);
        }

        /// <summary>
        /// 由環境變數載入主金鑰內容。
        /// </summary>
        /// <param name="varName">環境變數名稱。</param>
        /// <param name="autoCreate">是否自動建立主金鑰。</param>
        /// <returns>主金鑰內容。</returns>
        private static string LoadFromEnvironment(string varName, bool autoCreate)
        {
            if (string.IsNullOrWhiteSpace(varName))
            {
                varName = "BEE_MASTER_KEY";
            }

            string value = Environment.GetEnvironmentVariable(varName);

            if (string.IsNullOrWhiteSpace(value))
            {
                if (autoCreate)
                {
                    string newKey = GenerateNewKey();
                    Environment.SetEnvironmentVariable(varName, newKey);
                    return newKey;
                }
                throw new InvalidOperationException("Environment variable '" + varName + "' not found.");
            }

            return value;
        }

        /// <summary>
        /// 產生新的 Base64 編碼主金鑰。
        /// </summary>
        private static string GenerateNewKey()
        {
            // 產生新的 Base64 編碼主金鑰
            return AesCbcHmacKeyGenerator.GenerateBase64CombinedKey();
        }
    }

}
