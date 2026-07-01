using System.Globalization;
using Avalonia.Media;
using Bee.Base;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

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

        /// <summary>
        /// Gets or sets the client currency master for runtime decimal resolution of
        /// <see cref="NumberKind.Amount"/> fields. When <c>null</c> (the default), the editor uses the
        /// delivered/baked format — currency awareness is off, so existing non-amount behaviour is
        /// unchanged. Hosts set this (with <see cref="DefaultCurrencyCode"/>) to format amounts by the
        /// bound row's currency (see plan-numeric-multicurrency.md §3.2).
        /// </summary>
        public CurrencySettings? CurrencySettings { get; set; }

        /// <summary>
        /// Gets or sets the fallback currency code used when the bound row carries no currency-key field
        /// value (the master document / company default currency).
        /// </summary>
        public string DefaultCurrencyCode { get; set; } = string.Empty;

        // Amounts are not baked at delivery — when a currency master is supplied, resolve the format at
        // runtime from the bound row's currency. Otherwise prefer the delivered layout format (baked per
        // company), falling back to the schema field's format for ambient (field-name-only) binds.
        private string NumberFormat
        {
            get
            {
                var layoutField = Binder.LayoutField;
                if (CurrencySettings is not null && layoutField?.NumberKind == NumberKind.Amount)
                    return NumberFormatResolver.ResolveFormat(
                        NumberKind.Amount,
                        new RoundingContext { CurrencySettings = CurrencySettings },
                        ResolveCurrencyCode(layoutField));

                return layoutField?.NumberFormat is { Length: > 0 } layoutFormat
                    ? layoutFormat
                    : Binder.FormField?.NumberFormat ?? string.Empty;
            }
        }

        // Per-row currency: the field's currency-key field value on the bound row → the default currency.
        private string ResolveCurrencyCode(LayoutFieldBase layoutField)
        {
            var row = Binder.TargetRow;
            if (row is not null
                && StringUtilities.IsNotEmpty(layoutField.CurrencyField)
                && row.Table.Columns.Contains(layoutField.CurrencyField))
            {
                string code = ValueUtilities.CStr(row[layoutField.CurrencyField]);
                if (StringUtilities.IsNotEmpty(code)) { return code; }
            }
            return DefaultCurrencyCode;
        }

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
