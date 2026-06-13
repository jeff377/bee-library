namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Composes the lookup display text shared by <see cref="ButtonEdit"/> and the
    /// <see cref="GridControl"/> lookup cells.
    /// </summary>
    internal static class LookupDisplay
    {
        /// <summary>
        /// Separator between the display-field values. A plain space would be
        /// ambiguous because names themselves contain spaces (e.g. "Alice Chen"),
        /// so the id and name segments are joined as "E001 - Alice Chen".
        /// </summary>
        public const string Separator = " - ";

        /// <summary>
        /// Joins the non-empty display-field values with <see cref="Separator"/>.
        /// </summary>
        /// <param name="values">The display-field values in declaration order.</param>
        public static string Compose(IEnumerable<string> values)
            => string.Join(Separator, values.Where(v => !string.IsNullOrEmpty(v)));
    }
}
