using Bee.Base;

namespace Bee.Definition
{
    /// <summary>
    /// Framework-level named number format presets used by <c>FormField.NumberFormat</c>.
    /// Maps semantic preset names (e.g. <c>"Quantity"</c>, <c>"Amount"</c>) to .NET format strings.
    /// </summary>
    public static class NumberFormatPresets
    {
        /// <summary>
        /// Gets the .NET format string for the specified preset name.
        /// </summary>
        /// <param name="preset">The preset name.</param>
        /// <returns>The corresponding .NET format string, or empty when the preset is empty or unknown.</returns>
        public static string ToFormatString(string preset)
        {
            if (StrFunc.IsEmpty(preset))
                return string.Empty;
            else if (StrFunc.IsEquals(preset, "Quantity"))
                return "N0";
            else if (StrFunc.IsEquals(preset, "UnitPrice"))
                return "N2";
            else if (StrFunc.IsEquals(preset, "Amount"))
                return "N2";
            else if (StrFunc.IsEquals(preset, "Cost"))
                return "N4";
            else
                return string.Empty;
        }
    }
}
