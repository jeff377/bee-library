using System.Globalization;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Formats a raw field value into its display string, shared by <c>GridControl</c> cells and
    /// the <c>NumericEdit</c> editor so a value renders identically in a grid and in a form field.
    /// All formatting uses <see cref="CultureInfo.InvariantCulture"/> (the framework's canonical
    /// wire/display culture); an explicit <c>DisplayFormat</c> wins over the numeric format.
    /// </summary>
    internal static class CellValueFormatter
    {
        /// <summary>
        /// Formats <paramref name="raw"/> using <paramref name="displayFormat"/> when set, otherwise
        /// <paramref name="numberFormat"/>, falling back to a canonical string for dates and other
        /// formattable values. Returns an empty string for <c>null</c> / <see cref="DBNull"/>.
        /// </summary>
        /// <param name="raw">The raw field value.</param>
        /// <param name="displayFormat">An explicit display format string, or empty.</param>
        /// <param name="numberFormat">The numeric format string (e.g. <c>"N2"</c>), or empty.</param>
        public static string Format(object? raw, string displayFormat, string numberFormat)
        {
            if (raw is null || raw == DBNull.Value) return string.Empty;

            if (!string.IsNullOrEmpty(displayFormat) && raw is IFormattable formattableDisplay)
                return formattableDisplay.ToString(displayFormat, CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(numberFormat) && raw is IFormattable formattableNumber)
                return formattableNumber.ToString(numberFormat, CultureInfo.InvariantCulture);

            return raw switch
            {
                DateTime dt => dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => raw.ToString() ?? string.Empty,
            };
        }
    }
}
