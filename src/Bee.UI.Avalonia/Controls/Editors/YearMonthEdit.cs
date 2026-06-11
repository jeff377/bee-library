using Bee.Definition.Layouts;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Field editor for <see cref="ControlType.YearMonthEdit"/>: a <see cref="DateEdit"/>
    /// without the day column that binds as <c>yyyy-MM</c>.
    /// </summary>
    public class YearMonthEdit : DateEdit
    {
        /// <summary>
        /// Initializes a new instance of <see cref="YearMonthEdit"/>.
        /// </summary>
        public YearMonthEdit()
        {
            DayVisible = false;
        }

        /// <inheritdoc />
        protected override string ValueFormat => "yyyy-MM";
    }
}
