using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Shared building blocks for the read-only field appearance. Editors inheriting from
    /// non-text controls (<see cref="DatePicker"/>, <see cref="ComboBox"/>) cannot collapse
    /// their border to a bottom line with a setter, so in the read-only view they swap the
    /// whole control template for the minimal underlined display produced here.
    /// </summary>
    internal static class ReadOnlyFieldVisual
    {
        /// <summary>
        /// The constant light underline shared by every read-only field editor, so the
        /// text-box editors and the template-swapped editors render an identical bottom
        /// line. Set as a local value by callers so it beats the theme's hover/focus
        /// border setters and stays visible at rest. Semi-transparent grey reads as light
        /// on both light and dark variants.
        /// </summary>
        internal static readonly ImmutableSolidColorBrush UnderlineBrush =
            new(Color.FromArgb(0xB0, 0x80, 0x80, 0x80));

        // Left inset so the read-only value lines up with the TextBox-based editors, which
        // keep their theme content padding. The underline sits on the border edge (outside
        // this padding), so the inset shifts only the text, never the line width.
        private static readonly Thickness s_contentPadding = new(8, 5, 8, 5);

        /// <summary>
        /// The native control whose template the read-only display replaces. Each native
        /// control retrieves a different set of named parts in <c>OnApplyTemplate</c>, so
        /// the swapped template must register hidden stand-ins to avoid a crash.
        /// </summary>
        internal enum HostKind
        {
            /// <summary>A <see cref="TextBox"/>-like host that needs no extra parts.</summary>
            Plain,

            /// <summary>A <see cref="ComboBox"/> host (needs <c>PART_Popup</c>).</summary>
            ComboBox,

            /// <summary>A <see cref="DatePicker"/> host (needs the full button/text part set).</summary>
            DatePicker,
        }

        /// <summary>
        /// Builds the read-only display root: a transparent host showing <paramref name="text"/>
        /// over a single bottom line. Any named parts the host control's <c>OnApplyTemplate</c>
        /// dereferences are registered into <paramref name="scope"/> but kept out of the
        /// visual tree.
        /// </summary>
        /// <param name="text">The display text to render (observed live).</param>
        /// <param name="scope">The template name scope to register required parts into.</param>
        /// <param name="kind">The native control whose part contract must be satisfied.</param>
        internal static Control Build(IObservable<string?> text, INameScope scope, HostKind kind)
        {
            var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            label.Bind(TextBlock.TextProperty, text);

            // WARNING: The required parts below are registered into the name scope but NOT
            // added to this visual tree. A `Popup` is a popup-host control, not an ordinary
            // child; parenting one under a Panel freezes layout. The host control's
            // OnApplyTemplate only needs Find/Get to resolve the names, which registration
            // alone satisfies — the orphan parts are never measured, rendered or opened.
            switch (kind)
            {
                case HostKind.ComboBox:
                    RegisterComboBoxParts(scope);
                    break;
                case HostKind.DatePicker:
                    RegisterDatePickerParts(scope);
                    break;
            }

            return new Border
            {
                BorderBrush = UnderlineBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Background = Brushes.Transparent,
                Padding = s_contentPadding,
                Child = label,
            };
        }

        // ComboBox.OnApplyTemplate retrieves PART_Popup with NameScope.Get, which throws
        // when absent even though the read-only display never opens it.
        private static void RegisterComboBoxParts(INameScope scope)
        {
            scope.Register("PART_Popup", new Popup());
        }

        // DatePicker.OnApplyTemplate / SetSelectedDateText / SetGrid dereference these named
        // parts without null guards, so the swapped read-only template must register them.
        private static void RegisterDatePickerParts(INameScope scope)
        {
            var grid = new Grid();
            var day = new TextBlock();
            var month = new TextBlock();
            var year = new TextBlock();
            var firstSpacer = new Rectangle();
            var secondSpacer = new Rectangle();
            grid.Children.Add(day);
            grid.Children.Add(month);
            grid.Children.Add(year);
            grid.Children.Add(firstSpacer);
            grid.Children.Add(secondSpacer);

            scope.Register("PART_ButtonContentGrid", grid);
            scope.Register("PART_DayTextBlock", day);
            scope.Register("PART_MonthTextBlock", month);
            scope.Register("PART_YearTextBlock", year);
            scope.Register("PART_FirstSpacer", firstSpacer);
            scope.Register("PART_SecondSpacer", secondSpacer);
            scope.Register("PART_FlyoutButton", new Button());
            scope.Register("PART_Popup", new Popup());
        }
    }
}
