using System.ComponentModel;

namespace Bee.Web.Blazor.Server.UnitTests
{
    /// <summary>
    /// 骨架階段的煙霧測試：確認專案可編譯、組件可載入。
    /// </summary>
    public class SmokeTests
    {
        [Fact]
        [DisplayName("骨架階段組件可載入")]
        public void AssemblyLoads()
        {
            var assembly = typeof(SmokeTests).Assembly;
            Assert.NotNull(assembly);
        }
    }
}
