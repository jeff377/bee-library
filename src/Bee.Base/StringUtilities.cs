using System.Globalization;
using System.Text.RegularExpressions;

namespace Bee.Base
{
    /// <summary>
    /// Framework-level string utilities. The framework's design philosophy is to encapsulate
    /// ERP-context defaults (case-insensitive comparison, <see cref="CultureInfo.InvariantCulture"/>
    /// formatting, null-safe handling) inside these helpers so call sites do not have to
    /// pass <see cref="StringComparison"/> or <see cref="CultureInfo"/> repeatedly. Pass the
    /// optional <c>ignoreCase</c> parameter only when the call site needs different behavior.
    /// For prefix / suffix removal and out-parameter split, see <see cref="StringExtensions"/>.
    /// </summary>
    public static class StringUtilities
    {
        #region Empty / NotEmpty

        /// <summary>
        /// Determines whether the specified string is empty; null is treated as empty.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <param name="isTrim">Whether to trim leading and trailing whitespace before checking (default <c>true</c>).</param>
        public static bool IsEmpty(string? s, bool isTrim = true)
        {
            if (s is null) return true;
            return isTrim ? string.IsNullOrWhiteSpace(s) : s.Length == 0;
        }

        /// <summary>
        /// Casts the value to a string, then determines whether it is empty;
        /// null and DBNull are both treated as empty.
        /// </summary>
        /// <param name="value">The value to check.</param>
        public static bool IsEmpty(object? value)
        {
            return IsEmpty(ValueUtilities.CStr(value!));
        }

        /// <summary>
        /// Determines whether the specified string is not empty.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <param name="isTrim">Whether to trim leading and trailing whitespace before checking (default <c>true</c>).</param>
        public static bool IsNotEmpty(string? s, bool isTrim = true) => !IsEmpty(s, isTrim);

        /// <summary>
        /// Casts the value to a string, then determines whether it is not empty.
        /// </summary>
        /// <param name="value">The value to check.</param>
        public static bool IsNotEmpty(object? value) => !IsEmpty(value);

        #endregion

        #region Format

        /// <summary>
        /// Formats a string using the specified arguments. The framework enforces
        /// <see cref="CultureInfo.InvariantCulture"/> to avoid locale-dependent formatting issues
        /// (e.g. decimal separator differences) in ERP / database / serialization contexts.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">An array of arguments.</param>
        public static string Format(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);

        #endregion

        #region Equality / Containment (default IgnoreCase)

        /// <summary>
        /// Determines whether two strings are equal. Defaults to case-insensitive comparison —
        /// the framework convention for ERP business logic.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <param name="ignoreCase">Whether to ignore case (default <c>true</c>).</param>
        public static bool IsEquals(string? s1, string? s2, bool ignoreCase = true)
            => string.Equals(s1, s2, ignoreCase
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture);

        /// <summary>
        /// Determines whether the string equals any member of the comparison set
        /// (case-insensitive by default).
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <param name="values">The set of strings to compare against.</param>
        public static bool IsEqualsOr(string? s, params string[] values)
            => values.Any(v => IsEquals(s, v));

        /// <summary>
        /// Determines whether the string contains the specified substring.
        /// Defaults to case-insensitive search.
        /// </summary>
        /// <param name="s">The string to search in.</param>
        /// <param name="value">The substring to look for.</param>
        /// <param name="ignoreCase">Whether to ignore case (default <c>true</c>).</param>
        public static bool Contains(string? s, string value, bool ignoreCase = true)
        {
            if (s is null) return false;
            return s.Contains(value, ignoreCase
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Determines whether the string starts with the specified prefix.
        /// Defaults to case-insensitive comparison.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <param name="prefix">The prefix to look for.</param>
        /// <param name="ignoreCase">Whether to ignore case (default <c>true</c>).</param>
        public static bool StartsWith(string? s, string prefix, bool ignoreCase = true)
        {
            if (s is null) return false;
            return s.StartsWith(prefix, ignoreCase
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Determines whether the string ends with the specified suffix.
        /// Defaults to case-insensitive comparison.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <param name="suffix">The suffix to look for.</param>
        /// <param name="ignoreCase">Whether to ignore case (default <c>true</c>).</param>
        public static bool EndsWith(string? s, string suffix, bool ignoreCase = true)
        {
            if (s is null) return false;
            return s.EndsWith(suffix, ignoreCase
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Returns the zero-based index of the first occurrence of the substring.
        /// Returns -1 when not found or when the input is null.
        /// Defaults to case-insensitive search.
        /// </summary>
        /// <param name="s">The string to search in.</param>
        /// <param name="value">The substring to look for.</param>
        /// <param name="ignoreCase">Whether to ignore case (default <c>true</c>).</param>
        public static int IndexOf(string? s, string value, bool ignoreCase = true)
        {
            if (s is null) return -1;
            return s.IndexOf(value, ignoreCase
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Returns the zero-based index of the last occurrence of the substring.
        /// Returns -1 when not found or when the input is null.
        /// Defaults to case-insensitive search.
        /// </summary>
        /// <param name="s">The string to search in.</param>
        /// <param name="value">The substring to look for.</param>
        /// <param name="ignoreCase">Whether to ignore case (default <c>true</c>).</param>
        public static int LastIndexOf(string? s, string value, bool ignoreCase = true)
        {
            if (s is null) return -1;
            return s.LastIndexOf(value, ignoreCase
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture);
        }

        #endregion

        #region Replace / Trim / Split

        /// <summary>
        /// Replaces occurrences of a substring within a string using regex.
        /// Defaults to case-insensitive matching with a 1-second timeout.
        /// </summary>
        /// <param name="s">The string to process.</param>
        /// <param name="search">The substring to search for.</param>
        /// <param name="replacement">The replacement substring.</param>
        /// <param name="ignoreCase">Whether to ignore case (default <c>true</c>).</param>
        public static string Replace(string? s, string search, string replacement, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            return Regex.Replace(s, Regex.Escape(search), replacement, options, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Trims leading and trailing whitespace, plus invisible ZERO WIDTH SPACE (U+200B)
        /// and ZERO WIDTH NO-BREAK SPACE / BOM (U+FEFF).
        /// </summary>
        /// <param name="s">The string to trim.</param>
        public static string Trim(string? s)
        {
            if (s is null) return string.Empty;
            return s.Trim().Trim('﻿', '​');
        }

        /// <summary>
        /// Splits a string into an array using the specified delimiter.
        /// Returns an empty array when the input is null or whitespace.
        /// </summary>
        /// <param name="s">The string to split.</param>
        /// <param name="delimiter">The delimiter.</param>
        public static string[] Split(string? s, string delimiter)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
            return s.Split(new[] { delimiter }, StringSplitOptions.None);
        }

        #endregion

        #region GetNextId

        /// <summary>
        /// Gets the next sequential ID (supports base 2 to 36).
        /// </summary>
        /// <param name="value">The current ID.</param>
        /// <param name="numberBase">The base for the sequential ID (2-36).</param>
        public static string GetNextId(string value, int numberBase)
        {
            if (numberBase < 2 || numberBase > 36)
                throw new ArgumentOutOfRangeException(nameof(numberBase), "Number base must be between 2 and 36.");

            var baseValues = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(0, numberBase);
            return GetNextId(value, baseValues);
        }

        /// <summary>
        /// Gets the next sequential ID, incrementing according to a custom character set.
        /// </summary>
        /// <param name="value">The current ID.</param>
        /// <param name="baseValues">The character set defining the base.</param>
        public static string GetNextId(string value, string baseValues)
        {
            if (string.IsNullOrEmpty(baseValues))
                throw new ArgumentException("Base values must not be null or empty.", nameof(baseValues));

            var digits = baseValues.ToCharArray();
            var baseLength = digits.Length;
            var current = Trim(value).ToCharArray();

            for (int i = current.Length - 1; i >= 0; i--)
            {
                var index = Array.IndexOf(digits, current[i]);

                if (index == -1)
                    throw new ArgumentException($"Invalid character '{current[i]}' in current ID.", nameof(value));

                if (index < baseLength - 1)
                {
                    current[i] = digits[index + 1];
                    return new string(current);
                }

                // overflow → reset to first digit
                current[i] = digits[0];
            }

            // All digits overflowed; carry over by prepending the first non-zero character
            return digits[1] + new string(current);
        }

        #endregion
    }
}
