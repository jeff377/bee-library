using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Bee.Definition.Layouts;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Field editor for <see cref="ControlType.ButtonEdit"/>: a <see cref="TextEdit"/>
    /// with a trailing lookup icon, styled like the inner icon of a DatePicker. The
    /// lookup flow itself (open a picker, write mapped fields back) is the caller's
    /// responsibility through <see cref="ButtonClick"/>.
    /// </summary>
    /// <remarks>
    /// The lookup button follows <see cref="TextBox.IsReadOnly"/>: whenever the editor
    /// becomes read-only (View mode, a read-only layout field, or a direct assignment)
    /// the button is disabled, because the lookup flow writes mapped fields back.
    /// </remarks>
    public class ButtonEdit : TextEdit
    {
        // Magnifier glyph taken from Semi.Avalonia `SemiIconSearchStroked` (MIT),
        // embedded so the editor renders the same under any application theme.
        private const string SearchIconGeometry =
            "M16 10a6 6 0 1 1-12 0 6 6 0 0 1 12 0Zm-1.1 6.32a8 8 0 1 1 1.41-1.41l5.4 5.38a1 1 0 0 1-1.42 1.42l-5.38-5.39Z";

        private readonly PathIcon _icon;
        private readonly Button _button;

        /// <summary>
        /// Initializes a new instance of <see cref="ButtonEdit"/> with the embedded
        /// lookup icon.
        /// </summary>
        public ButtonEdit()
        {
            _icon = new PathIcon
            {
                Width = 14,
                Height = 14,
                // Match the muted tone date/time pickers use for their inner icon.
                Opacity = 0.65,
            };
            // Follow the editor's own text colour instead of the button theme's
            // foreground (accent-coloured under Semi), so the glyph stays the muted
            // grey of a DatePicker icon in both light and dark variants.
            _icon[!ForegroundProperty] = this[!ForegroundProperty];
            // A chromeless button keeps click/automation semantics while only the
            // glyph stays visible; local values beat the theme's hover/pressed
            // setters, so no button chrome shows up on interaction.
            _button = new Button
            {
                Content = _icon,
                Focusable = false,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(2),
                MinWidth = 0,
                MinHeight = 0,
            };
            _button.Click += (_, _) => ButtonClick?.Invoke(this, EventArgs.Empty);
            InnerRightContent = _button;
        }

        /// <summary>
        /// Raised when the embedded lookup icon is clicked.
        /// </summary>
        public event EventHandler? ButtonClick;

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            // A single property hook covers every path that turns the editor
            // read-only — `SetControlState`, layout metadata and direct assignment —
            // so the lookup button cannot fire `ButtonClick` on a read-only editor.
            if (change.Property == IsReadOnlyProperty)
                _button.IsEnabled = !IsReadOnly;
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            // `Geometry.Parse` and `Cursor` both need platform services that are
            // absent when unit tests construct editors without an Avalonia platform,
            // so they are created on first visual attach instead of in the constructor.
            _icon.Data ??= Geometry.Parse(SearchIconGeometry);
            _button.Cursor ??= new Cursor(StandardCursorType.Hand);
        }
    }
}
