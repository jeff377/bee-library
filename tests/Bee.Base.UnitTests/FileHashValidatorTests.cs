using System.ComponentModel;
using Bee.Base.Security;
using System.Text;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// FileHashValidator 測試類別。
    /// </summary>
    public class FileHashValidatorTests
    {
        private static string CreateTempFile(string content = "Hello Bee.NET")
        {
            var dir = Path.Combine(Path.GetTempPath(), "BeeBaseTests");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{Guid.NewGuid():N}.tmp");
            File.WriteAllText(path, content, Encoding.UTF8);
            return path;
        }

        [Fact]
        [DisplayName("計算 SHA256 雜湊後驗證應成功且不區分大小寫")]
        public void ComputeAndVerifySha256_ValidFile_VerificationSucceeds()
        {
            var path = CreateTempFile();
            try
            {
                var hex = FileHashValidator.ComputeSha256(path);
                Assert.True(FileHashValidator.VerifySha256(path, hex));
                Assert.True(FileHashValidator.VerifySha256(path, hex.ToLowerInvariant()));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        [DisplayName("VerifySha256 於雜湊值不符應回傳 false")]
        public void VerifySha256_MismatchedHash_ReturnsFalse()
        {
            var path = CreateTempFile();
            try
            {
                var wrongHash = new string('0', 64);
                Assert.False(FileHashValidator.VerifySha256(path, wrongHash));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        [DisplayName("VerifySha256 於預期 hex 長度不符應回傳 false")]
        public void VerifySha256_WrongLengthHex_ReturnsFalse()
        {
            var path = CreateTempFile();
            try
            {
                // 合法 hex 但長度不是 64(SHA-256 輸出的雙倍)
                Assert.False(FileHashValidator.VerifySha256(path, "ABCD"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        [DisplayName("VerifySha256 於 hex 長度為奇數應回傳 false")]
        public void VerifySha256_OddLengthHex_ReturnsFalse()
        {
            var path = CreateTempFile();
            try
            {
                Assert.False(FileHashValidator.VerifySha256(path, "ABC"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        [DisplayName("VerifySha256 於 hex 含非法字元應回傳 false")]
        public void VerifySha256_InvalidHexChars_ReturnsFalse()
        {
            var path = CreateTempFile();
            try
            {
                // 64 chars 但含非 hex 字元
                var invalidHex = "ZZ" + new string('0', 62);
                Assert.False(FileHashValidator.VerifySha256(path, invalidHex));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        [DisplayName("VerifySha256 於 hex 為空字串應回傳 false")]
        public void VerifySha256_EmptyHex_ReturnsFalse()
        {
            var path = CreateTempFile();
            try
            {
                Assert.False(FileHashValidator.VerifySha256(path, string.Empty));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        [DisplayName("VerifySha256 於檔案不存在應拋出 FileNotFoundException")]
        public void VerifySha256_MissingFile_ThrowsFileNotFoundException()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.missing");
            Assert.Throws<FileNotFoundException>(
                () => FileHashValidator.VerifySha256(path, new string('0', 64)));
        }

        [Fact]
        [DisplayName("VerifySha256 於路徑為空字串應拋出 FileNotFoundException")]
        public void VerifySha256_EmptyPath_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(
                () => FileHashValidator.VerifySha256(string.Empty, new string('0', 64)));
        }

        [Fact]
        [DisplayName("ComputeSha256 於檔案不存在應拋出 FileNotFoundException")]
        public void ComputeSha256_MissingFile_ThrowsFileNotFoundException()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.missing");
            Assert.Throws<FileNotFoundException>(() => FileHashValidator.ComputeSha256(path));
        }

        [Fact]
        [DisplayName("ComputeSha256 於路徑為空字串應拋出 FileNotFoundException")]
        public void ComputeSha256_EmptyPath_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => FileHashValidator.ComputeSha256(string.Empty));
        }
    }
}
