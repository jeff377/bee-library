using System;
using System.IO;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 提供初始化主加密金鑰的工具類別。
    /// 可從 master.key 檔案或環境變數載入 EncryptionKeySet，用於解密設定檔重要資訊。
    /// </summary>
    public static class EncryptionInitializer
    {
        /// <summary>
        /// 從指定的 master.key 檔案載入主加密金鑰組。
        /// </summary>
        /// <param name="filePath">主金鑰檔案的完整路徑。</param>
        /// <exception cref="FileNotFoundException">找不到 master.key 檔案。</exception>
        /// <exception cref="InvalidDataException">金鑰格式錯誤。</exception>
        public static void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Master.Key not found.", filePath);

            string base64 = File.ReadAllText(filePath).Trim();
            LoadFromBase64(base64);
        }

        /// <summary>
        /// 從指定的環境變數載入主加密金鑰組。
        /// </summary>
        /// <param name="envVarName">環境變數名稱。</param>
        /// <exception cref="InvalidOperationException">環境變數不存在或為空。</exception>
        /// <exception cref="InvalidDataException">金鑰格式錯誤。</exception>
        public static void LoadFromEnvironment(string envVarName)
        {
            string base64 = Environment.GetEnvironmentVariable(envVarName);
            if (string.IsNullOrWhiteSpace(base64))
                throw new InvalidOperationException($"Environment variable '{envVarName}' is not defined.");

            LoadFromBase64(base64.Trim());
        }

        /// <summary>
        /// 從 base64 編碼的金鑰字串載入 EncryptionKeySet，設定至 BackendInfo。
        /// </summary>
        /// <param name="base64">base64 編碼的 64-byte 合併金鑰字串。</param>
        /// <exception cref="InvalidDataException">格式錯誤或長度不符。</exception>
        public static void LoadFromBase64(string base64)
        {
            byte[] combined = Convert.FromBase64String(base64);
            if (combined.Length != 64)
                throw new InvalidDataException("Master.Key format is invalid. Expected 64 bytes.");

            BackendInfo.MasterEncryptionKeySet = new AesHmacKeySet(base64);
        }
    }

}
