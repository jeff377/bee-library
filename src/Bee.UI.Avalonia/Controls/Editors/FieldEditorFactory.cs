using Avalonia.Controls;
using Bee.Definition.Layouts;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Creates the field editor control that corresponds to a
    /// <see cref="ControlType"/> value.
    /// </summary>
    public static class FieldEditorFactory
    {
        /// <summary>
        /// Creates the field editor for <paramref name="controlType"/>. Every returned
        /// control implements <see cref="IFieldEditor"/>; <see cref="ControlType.Auto"/>
        /// and any unmapped value fall back to <see cref="TextEdit"/>.
        /// </summary>
        /// <param name="controlType">The layout control type.</param>
        public static Control Create(ControlType controlType)
        {
            return controlType switch
            {
                ControlType.CheckEdit => new CheckEdit(),
                ControlType.DateEdit => new DateEdit(),
                ControlType.YearMonthEdit => new YearMonthEdit(),
                ControlType.MemoEdit => new MemoEdit(),
                ControlType.DropDownEdit => new DropDownEdit(),
                ControlType.ButtonEdit => new ButtonEdit(),
                ControlType.NumericEdit => new NumericEdit(),
                _ => new TextEdit(),
            };
        }
    }
}
