using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// <c>Bee.Base</c> 不得長出「對外發起網路請求」的能力。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這個組件是**每一個**專案的相依 —— 含只讀定義檔的 UI head、離線的定義編輯工具、
    /// 產生器。放在這裡的任何能力都會出現在每個消費者的公開表面上，不管它用不用得到。
    /// <c>HttpUtilities</c> 曾住在這裡，而它的呼叫點從頭到尾都只在 <c>Bee.Api.Client</c>。
    /// </para>
    /// <para>
    /// 擋的是 <c>System.Net.Http</c>（對外開連線的那一套），不是整個 <c>System.Net</c>：
    /// <see cref="IPValidator"/> 用 <c>IPAddress</c> 做 CIDR 比對，那是值的解析與比對，
    /// 不會連到任何地方。把兩者混為一談的閘門會逼人為了通過而搬錯東西。
    /// </para>
    /// <para>
    /// 用 IL 參考而非 <c>deps.json</c>：<c>ImplicitUsings</c> 產生的
    /// <c>global using System.Net.Http;</c> 本身不會產生組件參考，真的用到型別才會。
    /// </para>
    /// </remarks>
    public class BaseLayerCapabilityGateTests
    {
        [Fact]
        [DisplayName("Bee.Base 不得參考 System.Net.Http（網路原語屬於上層）")]
        public void BaseAssembly_DoesNotReferenceHttpStack()
        {
            var referenced = typeof(IPValidator).Assembly
                .GetReferencedAssemblies()
                .Select(name => name.Name)
                .ToArray();

            // 對照組：確定真的讀到了參考清單，而不是空陣列讓斷言白過。
            Assert.Contains("System.Runtime", referenced, StringComparer.OrdinalIgnoreCase);

            Assert.DoesNotContain("System.Net.Http", referenced, StringComparer.OrdinalIgnoreCase);
        }
    }
}
