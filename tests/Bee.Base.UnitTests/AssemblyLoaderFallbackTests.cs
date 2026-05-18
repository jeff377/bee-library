using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    public class AssemblyLoaderFallbackTests
    {
        [Fact]
        [DisplayName("LoadAssembly 不存在的組件名稱（無目錄）應進入 FileNotFoundException fallback 並拋出例外")]
        public void LoadAssembly_UnknownNameNoDirectory_FallbackThrowsException()
        {
            var exception = Record.Exception(() => AssemblyLoader.LoadAssembly("BeeXyzNotExistFallback.dll"));
            Assert.NotNull(exception);
        }

        [Fact]
        [DisplayName("LoadAssembly 含目錄路徑的不存在組件應使用路徑直接作為 assemblyFile 並拋出例外")]
        public void LoadAssembly_PathWithDirectoryNotFound_FallbackThrowsException()
        {
            string fakeAssemblyPath = Path.Combine(
                Path.GetTempPath(), "bee_fake_dir_xyz", "BeeXyzNotExistFallback2.dll");
            var exception = Record.Exception(() => AssemblyLoader.LoadAssembly(fakeAssemblyPath));
            Assert.NotNull(exception);
        }
    }
}
