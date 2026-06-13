using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// Tests for <see cref="ButtonEdit"/> behavior independent of the lookup flow:
    /// ButtonClick event for non-relation fields, IsReadOnly → button enabled sync.
    /// </summary>
    public class ButtonEditTests
    {
        [Fact]
        [DisplayName("非 relation 欄位按下按鈕觸發 ButtonClick 事件")]
        public async Task OnButtonClickAsync_NoLookup_RaisesButtonClickEvent()
        {
            var editor = new ButtonEdit();
            var clicked = false;
            editor.ButtonClick += (_, _) => clicked = true;

            var method = typeof(ButtonEdit).GetMethod(
                "OnButtonClickAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            await (Task)method!.Invoke(editor, null)!;

            Assert.True(clicked);
        }

        [Fact]
        [DisplayName("非 lookup 欄位 IsReadOnly 變更時按鈕啟用狀態同步更新")]
        public void IsReadOnly_NoLookup_SyncsButtonEnabledState()
        {
            var editor = new ButtonEdit();
            var buttonField = typeof(ButtonEdit)
                .GetField("_button", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(buttonField);
            var button = (Button)buttonField!.GetValue(editor)!;

            editor.IsReadOnly = true;
            Assert.False(button.IsEnabled);

            editor.IsReadOnly = false;
            Assert.True(button.IsEnabled);
        }
    }
}
