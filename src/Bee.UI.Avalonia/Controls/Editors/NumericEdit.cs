using System.Globalization;
using Avalonia.Media;
using Bee.Definition.Layouts;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Field editor for <see cref="ControlType.NumericEdit"/>: a right-aligned numeric input that
    /// shows the value formatted per the field's <c>NumberFormat</c> at rest, reveals the raw
    /// full-precision value while focused for editing, and writes the parsed value back at full
    /// precision — the rounded display form is never written back. Partial or invalid input (for
    /// example <c>"12."</c>) keeps the last valid value.
    /// </summary>
    public class NumericEdit : TextEdit
    {
        // The bound value in its raw, full-precision invariant-culture string form. The display
        // Text may be a rounded rendering of this; write-backs always use the raw value.
        private string _rawValue = string.Empty;

        /// <summary>
        /// Initializes a new instance of <see cref="NumericEdit"/>.
        /// </summary>
        public NumericEdit()
        {
            TextAlignment = TextAlignment.Right;
            // Reveal full precision for editing; the display format only applies at rest.
            GotFocus += (_, _) =>
            {
                Text = _rawValue;
                SelectAll();
            };
            // Subscribed after the base TextEdit commit handler (registered in its constructor), so
            // this runs once the bound value is already written and can re-apply the display format.
            LostFocus += (_, _) => Text = FormatForDisplay(_rawValue);
        }

        // Prefer the delivered layout format (already baked per company), falling back to the schema
        // field's format for ambient (field-name-only) binds that carry no layout field.
        private string NumberFormat =>
            Binder.LayoutField?.NumberFormat is { Length: > 0 } layoutFormat
                ? layoutFormat
                : Binder.FormField?.NumberFormat ?? string.Empty;

        /// <inheritdoc />
        protected override void RefreshFromSource()
        {
            _rawValue = Binder.GetValue();
            Text = IsFocused ? _rawValue : FormatForDisplay(_rawValue);
        }

        /// <inheritdoc />
        protected override string? GetWriteBackValue()
        {
            // Write the parsed value at full precision; reject partial/invalid input by keeping the
            // last valid raw value so a stray keystroke never corrupts the bound field.
            if (TryParse(Text, out var value))
                _rawValue = value.ToString(CultureInfo.InvariantCulture);
            return _rawValue;
        }

        private string FormatForDisplay(string raw)
        {
            if (string.IsNullOrEmpty(NumberFormat) || !TryParse(raw, out var value))
                return raw;
            return CellValueFormatter.Format(value, string.Empty, NumberFormat);
        }

        private static bool TryParse(string? text, out decimal value)
            => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
