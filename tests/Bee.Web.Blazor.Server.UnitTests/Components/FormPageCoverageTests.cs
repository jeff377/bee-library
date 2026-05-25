using System.ComponentModel;
using System.Reflection;
using Bee.Web.Blazor.Server.Components;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// 補強 <see cref="FormPage.OnInitializedAsync"/> 早期返回路徑的測試覆蓋率。
    /// ProgId 為空時在存取 Factory 之前即提前返回，不需要 Blazor 渲染器支援。
    /// </summary>
    public class FormPageCoverageTests
    {
        private static FieldInfo GetErrorField() =>
            typeof(FormPage).GetField("_error", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static FieldInfo GetIsInitializingField() =>
            typeof(FormPage).GetField("_isInitializing", BindingFlags.NonPublic | BindingFlags.Instance)!;

        [Fact]
        [DisplayName("OnInitializedAsync ProgId 為空字串時應設定錯誤訊息")]
        public async Task OnInitializedAsync_EmptyProgId_SetsErrorMessage()
        {
            var page = new FormPage();
            var method = typeof(FormPage).GetMethod(
                "OnInitializedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, null)!;
            await task;
            var error = GetErrorField().GetValue(page) as string;
            Assert.Equal("FormPage.ProgId must be set.", error);
        }

        [Fact]
        [DisplayName("OnInitializedAsync ProgId 為空字串時應將 _isInitializing 設為 false")]
        public async Task OnInitializedAsync_EmptyProgId_SetsIsInitializingFalse()
        {
            var page = new FormPage();
            var method = typeof(FormPage).GetMethod(
                "OnInitializedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, null)!;
            await task;
            Assert.False((bool)GetIsInitializingField().GetValue(page)!);
        }

        [Fact]
        [DisplayName("OnInitializedAsync ProgId 為空白字串時應設定錯誤訊息")]
        public async Task OnInitializedAsync_WhitespaceProgId_SetsErrorMessage()
        {
            var page = new FormPage { ProgId = "   " };
            var method = typeof(FormPage).GetMethod(
                "OnInitializedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, null)!;
            await task;
            var error = GetErrorField().GetValue(page) as string;
            Assert.Equal("FormPage.ProgId must be set.", error);
        }
    }
}
