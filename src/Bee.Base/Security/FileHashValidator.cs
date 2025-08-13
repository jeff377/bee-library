using System;
using System.IO;
using System.Security.Cryptography;

namespace Bee.Base
{
    /// <summary>
    /// 提供檔案雜湊值的計算與驗證功能。
    /// </summary>
    public static class FileHashValidator
    {
        /// <summary>
        /// 驗證檔案的 SHA256 是否與指定的雜湊值相符。
        /// </summary>
        /// <param name="filePath">要驗證的檔案完整路徑。</param>
        /// <param name="expectedSha256Hex">預期的 SHA256（十六進位字串，不分大小寫）。</param>
        /// <returns>相符則為 true，否則為 false。</returns>
        public static bool VerifySha256(string filePath, string expectedSha256Hex)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                var actualHex = BitConverter.ToString(hashBytes).Replace("-", "");
                return string.Equals(actualHex, expectedSha256Hex, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// 計算檔案的 SHA256 雜湊值（十六進位字串）。
        /// </summary>
        /// <param name="filePath">檔案完整路徑。</param>
        /// <returns>檔案 SHA256（十六進位字串）。</returns>
        public static string ComputeSha256(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
        }
    }
}
