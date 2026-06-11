using Avalonia.Controls;
using Bee.Definition.Layouts;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Field editor for <see cref="ControlType.ButtonEdit"/>: a <see cref="TextEdit"/>
    /// with an embedded trailing button. The lookup flow itself (open a picker, write
    /// mapped fields back) is the caller's responsibility through <see cref="ButtonClick"/>.
    /// </summary>
    public class ButtonEdit : TextEdit
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ButtonEdit"/> with the embedded button.
        /// </summary>
        public ButtonEdit()
        {
            var button = new Button
            {
                Content = "…",
                Focusable = false,
            };
            button.Click += (_, _) => ButtonClick?.Invoke(this, EventArgs.Empty);
            InnerRightContent = button;
        }

        /// <summary>
        /// Raised when the embedded button is clicked.
        /// </summary>
        public event EventHandler? ButtonClick;
    }
}
