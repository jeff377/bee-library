using System.ComponentModel;
using Bee.Definition.Security;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Security
{
    public class MasterKeyProviderRetryTests
    {
        [Fact]
        [DisplayName("GetMasterKey autoCreate=true 父目錄不存在時應觸發 catch(IOException) fallthrough 並於重試耗盡後拋出例外")]
        public void GetMasterKey_AutoCreate_ParentDirMissing_TriggersIoExceptionFallback()
        {
            // 父目錄（bee-missing-<guid>）不存在：
            //   FileStream(FileMode.CreateNew) → DirectoryNotFoundException（IOException 子類）
            //   → LoadFromFile 的 catch(IOException) 捕獲，fallthrough 到 ReadAllTextShared
            //   → ReadAllTextShared 逐次重試（ReadRetryCount=5，每次 Thread.Sleep 50ms），
            //     第 5 次 when (attempt < 4) 為 false，例外向上傳播。
            // 預期耗時約 200ms（4 × 50ms sleep）。
            string missingParent = Path.Combine(
                Path.GetTempPath(),
                $"bee-missing-{Guid.NewGuid():N}",
                "file.key");

            var source = new MasterKeySource { Type = MasterKeySourceType.File, Value = missingParent };

            var exception = Record.Exception(() =>
                MasterKeyProvider.GetMasterKey(source, definePath: string.Empty, autoCreate: true));

            Assert.NotNull(exception);
            Assert.IsAssignableFrom<IOException>(exception);
        }
    }
}
