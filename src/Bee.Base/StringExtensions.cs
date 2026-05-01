namespace Bee.Base
{
    /// <summary>
    /// Extension methods for <see cref="string"/> covering operations BCL does not provide:
    /// out-parameter split (<see cref="SplitLeft"/> / <see cref="SplitRight"/>) and
    /// conditional prefix / suffix removal (<see cref="LeftCut"/> / <see cref="RightCut"/> /
    /// <see cref="LeftRightCut"/>). All comparisons default to case-insensitive (the framework
    /// convention for ERP business logic).
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Searches for the delimiter from the left and splits the string into left and right parts.
        /// Both parts are empty when the delimiter is not found. Comparison is case-insensitive.
        /// </summary>
        /// <param name="s">The string to split.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="left">The output left portion of the string.</param>
        /// <param name="right">The output right portion of the string.</param>
        public static void SplitLeft(this string? s, string delimiter, out string left, out string right)
        {
            int pos = string.IsNullOrEmpty(s)
                ? -1
                : s.IndexOf(delimiter, StringComparison.CurrentCultureIgnoreCase);
            (left, right) = SliceAt(s, pos, delimiter.Length);
        }

        /// <summary>
        /// Searches for the delimiter from the right and splits the string into left and right parts.
        /// Both parts are empty when the delimiter is not found. Comparison is case-insensitive.
        /// </summary>
        /// <param name="s">The string to split.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="left">The output left portion of the string.</param>
        /// <param name="right">The output right portion of the string.</param>
        public static void SplitRight(this string? s, string delimiter, out string left, out string right)
        {
            int pos = string.IsNullOrEmpty(s)
                ? -1
                : s.LastIndexOf(delimiter, StringComparison.CurrentCultureIgnoreCase);
            (left, right) = SliceAt(s, pos, delimiter.Length);
        }

        private static (string left, string right) SliceAt(string? s, int pos, int delimiterLength)
        {
            if (string.IsNullOrEmpty(s) || pos < 0)
                return (string.Empty, string.Empty);
            string left = s.Substring(0, pos);
            int rightStart = pos + delimiterLength;
            string right = rightStart >= s.Length ? string.Empty : s.Substring(rightStart);
            return (left, right);
        }

        /// <summary>
        /// Removes the specified prefix from the start of the string if present.
        /// Match is case-insensitive.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="prefix">The prefix string to remove.</param>
        public static string LeftCut(this string? s, string prefix)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase)
                ? s.Substring(prefix.Length)
                : s;
        }

        /// <summary>
        /// Removes the specified suffix from the end of the string if present.
        /// Match is case-insensitive.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="suffix">The suffix string to remove.</param>
        public static string RightCut(this string? s, string suffix)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase)
                ? s.Substring(0, s.Length - suffix.Length)
                : s;
        }

        /// <summary>
        /// Removes the specified prefix and suffix from both ends of the string if present.
        /// Match is case-insensitive.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="prefix">The prefix to remove.</param>
        /// <param name="suffix">The suffix to remove.</param>
        public static string LeftRightCut(this string? s, string prefix, string suffix)
            => s.LeftCut(prefix).RightCut(suffix);
    }
}
