using System.ComponentModel;
using System.Reflection;
using Bee.Web.Blazor.Server.Components;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// 補強 <see cref="BeeLoginPanel.OnSubmitAsync"/> 兩條可在無 DI 環境下觸發的路徑：
    /// 1. _isBusy guard（直接返回，不觸碰 Factory）
    /// 2. Factory 未注入時的 try-catch-finally 路徑（NRE 被 catch 接住）
    /// </summary>
    public class BeeLoginPanelInternalTests
    {
        private static FieldInfo GetBusyField() =>
            typeof(BeeLoginPanel).GetField("_isBusy", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static FieldInfo GetErrorField() =>
            typeof(BeeLoginPanel).GetField("_error", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static async Task InvokeOnSubmitAsync(BeeLoginPanel panel)
        {
            var method = typeof(BeeLoginPanel).GetMethod(
                "OnSubmitAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(panel, null)!;
            await task;
        }

        [Fact]
        [DisplayName("OnSubmitAsync _isBusy 為 true 時應直接返回，不呼叫 Factory 且不拋例外")]
        public async Task OnSubmitAsync_WhenBusy_ReturnsImmediatelyWithoutError()
        {
            var panel = new BeeLoginPanel();
            var busyField = GetBusyField();
            busyField.SetValue(panel, true);
            var exception = await Record.ExceptionAsync(() => InvokeOnSubmitAsync(panel));
            Assert.Null(exception);
            Assert.True((bool)busyField.GetValue(panel)!);
        }

        [Fact]
        [DisplayName("OnSubmitAsync Factory 未注入時應進入 catch 設定錯誤訊息，且 _isBusy 恢復 false")]
        public async Task OnSubmitAsync_NullFactory_CatchesExceptionAndResetsBusy()
        {
            var panel = new BeeLoginPanel();
            await InvokeOnSubmitAsync(panel);
            Assert.False((bool)GetBusyField().GetValue(panel)!);
            Assert.NotNull(GetErrorField().GetValue(panel) as string);
        }
    }
}
