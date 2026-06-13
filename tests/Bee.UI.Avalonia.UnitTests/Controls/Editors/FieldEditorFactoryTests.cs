using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// Verifies the <see cref="FieldEditorFactory"/> ControlType dispatch and the
    /// StyleKey contract that keeps every editor on its native control's theme.
    /// </summary>
    public class FieldEditorFactoryTests
    {
        [Theory]
        [InlineData(ControlType.TextEdit, typeof(TextEdit))]
        [InlineData(ControlType.MemoEdit, typeof(MemoEdit))]
        [InlineData(ControlType.ButtonEdit, typeof(ButtonEdit))]
        [InlineData(ControlType.DateEdit, typeof(DateEdit))]
        [InlineData(ControlType.YearMonthEdit, typeof(YearMonthEdit))]
        [InlineData(ControlType.DropDownEdit, typeof(DropDownEdit))]
        [InlineData(ControlType.CheckEdit, typeof(CheckEdit))]
        [InlineData(ControlType.Auto, typeof(TextEdit))]
        [DisplayName("FieldEditorFactory 依 ControlType 建立對應編輯器（Auto fallback 為 TextEdit）")]
        public void Create_ControlType_ReturnsMatchingEditor(ControlType controlType, Type expectedType)
        {
            var editor = FieldEditorFactory.Create(controlType);

            Assert.IsType(expectedType, editor);
            Assert.IsType<IFieldEditor>(editor, false);
        }

        [Theory]
        [InlineData(typeof(TextEdit), typeof(TextBox))]
        [InlineData(typeof(MemoEdit), typeof(TextBox))]
        [InlineData(typeof(ButtonEdit), typeof(TextBox))]
        [InlineData(typeof(DateEdit), typeof(DatePicker))]
        [InlineData(typeof(YearMonthEdit), typeof(DatePicker))]
        [InlineData(typeof(DropDownEdit), typeof(ComboBox))]
        [InlineData(typeof(CheckEdit), typeof(CheckBox))]
        [DisplayName("各編輯器 StyleKeyOverride 指向原生基底（防隱形控件回歸）")]
        public void StyleKeyOverride_Editor_PointsToNativeBase(Type editorType, Type expectedStyleKey)
        {
            var editor = (Control)Activator.CreateInstance(editorType)!;
            var styleKey = typeof(global::Avalonia.StyledElement)
                .GetProperty("StyleKeyOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(editor);

            Assert.Equal(expectedStyleKey, styleKey);
        }
    }
}
