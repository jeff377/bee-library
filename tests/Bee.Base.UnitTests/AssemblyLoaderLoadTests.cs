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
    }
}
