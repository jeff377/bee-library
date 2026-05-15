using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    public class AssemblyLoaderLoadTests
    {
        [Fact]
        [DisplayName("LoadAssembly 傳入不存在的組件名稱應拋出例外（涵蓋 FileNotFoundException 回退路徑）")]
        public void LoadAssembly_NonExistentAssembly_ThrowsException()
        {
            var ex = Record.Exception(() => AssemblyLoader.LoadAssembly("Does.Not.Exist.XyzAbc.dll"));
            Assert.NotNull(ex);
        }

        [Fact]
        [DisplayName("LoadAssembly 傳入已在 AppDomain 中的組件名稱但以簡單名稱格式應正確載入")]
        public void LoadAssembly_KnownAssemblyBySimpleName_ReturnsAssembly()
        {
            // Bee.Definition 已在執行期間載入（測試專案相依）
            var assembly = AssemblyLoader.LoadAssembly("Bee.Definition.dll");
            Assert.NotNull(assembly);
            Assert.Equal("Bee.Definition", assembly.GetName().Name);
        }
    }
}
