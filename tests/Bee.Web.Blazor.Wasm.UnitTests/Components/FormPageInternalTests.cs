using System.ComponentModel;
using System.Reflection;
using Bee.Web.Blazor.Wasm.Components;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// 補強 <see cref="FormPage"/> 私有方法的覆蓋率。
    /// 測試範圍：<c>RunGuardedAsync</c> 的三種路徑（成功、拋例外、Busy Guard），
    /// 以及四個 Action handler 在 <c>_dataObject</c> 為 null 時的提前返回行為。
    /// 需要 Blazor 渲染器或 API connector 的 lifecycle 測試留待 bUnit 整合測試覆蓋。
    /// </summary>
    public class FormPageInternalTests
    {
        private static MethodInfo GetRunGuardedAsync()
        {
            var method = typeof(FormPage).GetMethod(
                "RunGuardedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return method!;
        }

        private static FieldInfo GetErrorField()
        {
            var field = typeof(FormPage).GetField(
                "_error", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return field!;
        }

        private static FieldInfo GetBusyField()
        {
            var field = typeof(FormPage).GetField(
                "_isBusy", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return field!;
        }

        // ──────────────────────────────────────────────────────────────
        // RunGuardedAsync
        // ──────────────────────────────────────────────────────────────

        [Fact]
        [DisplayName("RunGuardedAsync 執行成功的 action 後 _isBusy 應恢復為 false")]
        public async Task RunGuardedAsync_SuccessfulAction_ExecutesAndResetsBusy()
        {
            var page = new FormPage();
            var method = GetRunGuardedAsync();
            var busyField = GetBusyField();
            bool executed = false;
            Func<Task> action = () =>
            {
                executed = true;
                return Task.CompletedTask;
            };
            var task = (Task)method.Invoke(page, new object[] { action })!;
            await task;
            Assert.True(executed);
            Assert.False((bool)busyField.GetValue(page)!);
        }

        [Fact]
        [DisplayName("RunGuardedAsync 執行拋例外的 action 後 _error 應被設定，_isBusy 應恢復 false")]
        public async Task RunGuardedAsync_ThrowingAction_SetsErrorAndResetsBusy()
        {
            var page = new FormPage();
            var method = GetRunGuardedAsync();
            var errorField = GetErrorField();
            var busyField = GetBusyField();
            Func<Task> action = () => throw new InvalidOperationException("test error");
            var task = (Task)method.Invoke(page, new object[] { action })!;
            await task;
            Assert.Equal("test error", errorField.GetValue(page) as string);
            Assert.False((bool)busyField.GetValue(page)!);
        }

        [Fact]
        [DisplayName("RunGuardedAsync 在 _isBusy=true 時呼叫應跳過 action 不執行")]
        public async Task RunGuardedAsync_WhenBusy_SkipsAction()
        {
            var page = new FormPage();
            var method = GetRunGuardedAsync();
            var busyField = GetBusyField();
            busyField.SetValue(page, true);
            bool executed = false;
            Func<Task> action = () =>
            {
                executed = true;
                return Task.CompletedTask;
            };
            try
            {
                var task = (Task)method.Invoke(page, new object[] { action })!;
                await task;
                Assert.False(executed);
            }
            finally
            {
                busyField.SetValue(page, false);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Null _dataObject guard（私有 Action handler 提前返回）
        // ──────────────────────────────────────────────────────────────

        [Fact]
        [DisplayName("OnRowSelectedAsync _dataObject 為 null 時應提前返回，不拋例外")]
        public async Task OnRowSelectedAsync_NullDataObject_ReturnsWithoutError()
        {
            var page = new FormPage();
            var method = typeof(FormPage).GetMethod(
                "OnRowSelectedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, new object[] { Guid.NewGuid() })!;
            var exception = await Record.ExceptionAsync(() => task);
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("OnNewAsync _dataObject 為 null 時應提前返回，不拋例外")]
        public async Task OnNewAsync_NullDataObject_ReturnsWithoutError()
        {
            var page = new FormPage();
            var method = typeof(FormPage).GetMethod(
                "OnNewAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, null)!;
            var exception = await Record.ExceptionAsync(() => task);
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("OnSaveAsync _dataObject 為 null 時應提前返回，不拋例外")]
        public async Task OnSaveAsync_NullDataObject_ReturnsWithoutError()
        {
            var page = new FormPage();
            var method = typeof(FormPage).GetMethod(
                "OnSaveAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, null)!;
            var exception = await Record.ExceptionAsync(() => task);
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("OnDeleteAsync _dataObject 為 null 時應提前返回，不拋例外")]
        public async Task OnDeleteAsync_NullDataObject_ReturnsWithoutError()
        {
            var page = new FormPage();
            var method = typeof(FormPage).GetMethod(
                "OnDeleteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, null)!;
            var exception = await Record.ExceptionAsync(() => task);
            Assert.Null(exception);
        }
    }
}
