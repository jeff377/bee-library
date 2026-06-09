using System.ComponentModel;
using Bee.Web.Blazor.Wasm.Components;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// 補強 BeeAccessTokenProvider.razor @ChildContent?.Invoke(this) 渲染行的覆蓋率。
    /// </summary>
    public class BeeAccessTokenProviderRenderTests : BunitContext
    {
        [Fact]
        [DisplayName("BeeAccessTokenProvider 傳入 ChildContent 時應渲染子內容")]
        public void BeeAccessTokenProvider_WithChildContent_RendersChildContent()
        {
            RenderFragment<BeeAccessTokenProvider> childContent =
                _ => builder => builder.AddContent(0, "child-content-test");

            var cut = Render<BeeAccessTokenProvider>(p => p
                .Add(c => c.ChildContent, childContent));

            Assert.Contains("child-content-test", cut.Markup);
        }
    }
}
