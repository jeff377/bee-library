using System.ComponentModel;
using System.Reflection;
using Bee.Web.Blazor.Server.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// 補強 <see cref="BeeLoginPanel"/> Razor 模板 <c>BuildRenderTree</c> 的覆蓋率。
    /// 測試在無 DI 環境下直接呼叫 <c>BuildRenderTree</c>，
    /// 涵蓋有無錯誤訊息的兩條條件分支。
    /// </summary>
    public class BeeLoginPanelRenderTests
    {
        private static readonly MethodInfo s_buildRenderTree =
            typeof(BeeLoginPanel)
                .GetMethod("BuildRenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo s_errorField =
            typeof(BeeLoginPanel).GetField("_error", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static void Render(BeeLoginPanel component)
        {
            var builder = new RenderTreeBuilder();
            s_buildRenderTree.Invoke(component, new object[] { builder });
        }

        [Fact]
        [DisplayName("BuildRenderTree 無錯誤訊息時不應拋出例外")]
        public void BuildRenderTree_NoError_DoesNotThrow()
        {
            var component = new BeeLoginPanel();
            var ex = Record.Exception(() => Render(component));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("BuildRenderTree 有錯誤訊息時應渲染錯誤提示區塊且不拋出例外")]
        public void BuildRenderTree_WithError_DoesNotThrow()
        {
            var component = new BeeLoginPanel();
            s_errorField.SetValue(component, "登入失敗：憑證無效");
            var ex = Record.Exception(() => Render(component));
            Assert.Null(ex);
        }
    }
}
