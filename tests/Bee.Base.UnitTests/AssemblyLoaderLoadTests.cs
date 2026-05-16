using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// 補強 <see cref="AssemblyLoader.LoadAssembly"/> 中 Assembly.Load 路徑的覆蓋率測試。
    /// 使用不含 .dll 副檔名的組件名稱，繞過 FindAssembly 的快取與 AppDomain 查找，
    /// 直接進入 Assembly.Load(AssemblyName) 流程。
    /// </summary>
    public class AssemblyLoaderLoadTests
    {
        [Fact]
        [DisplayName("LoadAssembly 使用不含 .dll 副檔名的名稱應成功載入並回傳正確組件")]
        public void LoadAssembly_WithoutDllExtension_ReturnsAssembly()
        {
            // "Bee.Base"（無 .dll）不符合 ManifestModule.Name="Bee.Base.dll" 的 AppDomain 比對，
            // 因此跳過快取與 AppDomain 搜尋，觸發 Assembly.Load(new AssemblyName("Bee.Base")) 路徑。
            var assembly = AssemblyLoader.LoadAssembly("Bee.Base");

            Assert.NotNull(assembly);
            Assert.Equal("Bee.Base", assembly.GetName().Name);
        }

        [Fact]
        [DisplayName("LoadAssembly 使用不含 .dll 副檔名重複呼叫應回傳相同實例（快取命中）")]
        public void LoadAssembly_WithoutDllExtension_RepeatedCall_ReturnsSameInstance()
        {
            var first = AssemblyLoader.LoadAssembly("Bee.Base");
            var second = AssemblyLoader.LoadAssembly("Bee.Base");

            Assert.Same(first, second);
        }
    }
}
