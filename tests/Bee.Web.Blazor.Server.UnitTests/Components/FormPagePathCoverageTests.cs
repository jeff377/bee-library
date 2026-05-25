using System.ComponentModel;
using System.Reflection;
using Bee.Web.Blazor.Server.Components;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// 補強 <see cref="FormPage.OnInitializedAsync"/> 有效 ProgId 路徑的覆蓋率。
    /// Factory 未注入（null）時，try 區塊內拋出 NullReferenceException，
    /// 由 catch 接住並設定 _error，finally 重設 _isInitializing，
    /// 無需 Blazor 渲染器即可完整覆蓋 try-catch-finally 三個分支。
    /// </summary>
    public class FormPagePathCoverageTests
    {
        private static FieldInfo GetErrorField() =>
            typeof(FormPage).GetField("_error", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static FieldInfo GetIsInitializingField() =>
            typeof(FormPage).GetField("_isInitializing", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static async Task InvokeOnInitializedAsync(FormPage page)
        {
            var method = typeof(FormPage).GetMethod(
                "OnInitializedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, null)!;
            await task;
        }

        private static FormPage CreatePageWithProgId(string progId)
        {
            var page = new FormPage();
            typeof(FormPage).GetProperty("ProgId",
                BindingFlags.Public | BindingFlags.Instance)!.SetValue(page, progId);
            return page;
        }

        [Fact]
        [DisplayName("OnInitializedAsync 有效 ProgId 且 Factory 未注入時應進入 catch 並設定錯誤訊息")]
        public async Task OnInitializedAsync_ValidProgIdNullFactory_CatchesExceptionAndSetsError()
        {
            var page = CreatePageWithProgId("TestProg");
            await InvokeOnInitializedAsync(page);
            var error = GetErrorField().GetValue(page) as string;
            Assert.NotNull(error);
        }

        [Fact]
        [DisplayName("OnInitializedAsync 有效 ProgId 且 Factory 未注入時 finally 應將 _isInitializing 設為 false")]
        public async Task OnInitializedAsync_ValidProgIdNullFactory_FinallyResetsIsInitializing()
        {
            var page = CreatePageWithProgId("TestProg");
            await InvokeOnInitializedAsync(page);
            Assert.False((bool)GetIsInitializingField().GetValue(page)!);
        }
    }
}
