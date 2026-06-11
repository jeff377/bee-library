using Avalonia.Media;
using Bee.Definition.Layouts;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Field editor for <see cref="ControlType.MemoEdit"/>: a multi-line
    /// <see cref="TextEdit"/> that accepts returns and wraps text.
    /// </summary>
    public class MemoEdit : TextEdit
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MemoEdit"/>.
        /// </summary>
        public MemoEdit()
        {
            AcceptsReturn = true;
            TextWrapping = TextWrapping.Wrap;
            MinHeight = 60;
        }
    }
}
