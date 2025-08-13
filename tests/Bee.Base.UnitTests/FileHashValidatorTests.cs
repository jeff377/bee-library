using System.Text;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// FileHashValidator 測試類別。
    /// </summary>
    public class FileHashValidatorTests
    {
        [Fact]
        public void ComputeThenVerifySha256_Succeeds()
        {
            // Arrange
            var dir = Path.Combine(Path.GetTempPath(), "BeeBaseTests");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{Guid.NewGuid():N}.tmp");
            File.WriteAllText(path, "Hello Bee.NET", Encoding.UTF8);

            try
            {
                // Act：計算 SHA256（十六進位）
                var hex = FileHashValidator.ComputeSha256(path);

                // Assert：使用相同雜湊值驗證應為 true
                Assert.True(FileHashValidator.VerifySha256(path, hex));

                // 也驗證大小寫不敏感
                Assert.True(FileHashValidator.VerifySha256(path, hex.ToLowerInvariant()));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
