using System.Collections.Immutable;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// The field data types accepted by <c>FormField/@DbType</c> and <c>DbField/@DbType</c>.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: These values duplicate the <c>Bee.Definition.FieldDbType</c> enumeration. The
    /// duplication is unavoidable because analyzers target netstandard2.0 and cannot reference the
    /// net10.0 framework assembly. <c>FieldDbTypesSyncTests</c> asserts that this list stays equal to
    /// the enumeration, so adding a member there fails the build until it is added here too.
    /// </remarks>
    internal static class FieldDbTypes
    {
        /// <summary>
        /// All accepted data type names, in enumeration order.
        /// </summary>
        public static readonly ImmutableArray<string> All = ImmutableArray.Create(
            "String",
            "Text",
            "Boolean",
            "AutoIncrement",
            "Short",
            "Integer",
            "Long",
            "Decimal",
            "Currency",
            "Date",
            "DateTime",
            "Guid",
            "Binary",
            "Unknown",
            "Time");

        /// <summary>
        /// Determines whether the specified value names an accepted data type.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns><c>true</c> when the value matches a data type name exactly.</returns>
        /// <remarks>
        /// Comparison is ordinal because the value is deserialised into an enumeration by
        /// <c>XmlSerializer</c>, which is itself case-sensitive; a differently cased value fails to
        /// deserialise rather than being coerced.
        /// </remarks>
        public static bool IsValid(string value)
        {
            foreach (var name in All)
            {
                if (string.Equals(value, name, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the accepted data type name that differs from the specified value only by case.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>The correctly cased name, or <c>null</c> when no name matches.</returns>
        public static string? FindCaseInsensitiveMatch(string value)
        {
            foreach (var name in All)
            {
                if (string.Equals(value, name, StringComparison.OrdinalIgnoreCase))
                    return name;
            }

            return null;
        }
    }
}
