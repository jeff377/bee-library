using System.ComponentModel;
using Bee.Base.Security;
using Bee.Definition.Security;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Security
{
    public class MasterKeyProviderRelativePathTests
    {
        [Fact]
        [DisplayName("GetMasterKey 相對路徑應套用 definePath 組合成絕對路徑")]
        public void GetMasterKey_RelativeFilePath_ResolvesAgainstDefinePath()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-mk-rel-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            string fileName = "relative.key";
            string fullPath = Path.Combine(tempDir, fileName);
            byte[] expected = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            File.WriteAllText(fullPath, Convert.ToBase64String(expected));

            try
            {
                byte[] actual = MasterKeyProvider.GetMasterKey(
                    new MasterKeySource { Type = MasterKeySourceType.File, Value = fileName },
                    definePath: tempDir);

                Assert.Equal(expected, actual);
            }
            finally
            {
                if (File.Exists(fullPath)) File.Delete(fullPath);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, false);
            }
        }

        [Fact]
        [DisplayName("GetMasterKey 相對路徑不存在且 autoCreate=true 應在 definePath 下建立檔案")]
        public void GetMasterKey_RelativeFilePath_Missing_AutoCreate_CreatesInDefinePath()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-mk-relcreate-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            string fileName = "auto-created.key";
            string fullPath = Path.Combine(tempDir, fileName);

            try
            {
                byte[] result = MasterKeyProvider.GetMasterKey(
                    new MasterKeySource { Type = MasterKeySourceType.File, Value = fileName },
                    definePath: tempDir,
                    autoCreate: true);

                Assert.NotEmpty(result);
                Assert.True(File.Exists(fullPath));
            }
            finally
            {
                if (File.Exists(fullPath)) File.Delete(fullPath);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, false);
            }
        }
    }
}
