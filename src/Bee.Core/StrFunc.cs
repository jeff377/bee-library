using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.Globalization;

namespace Bee.Core
{
    /// <summary>
    /// Utility library for string operations.
    /// </summary>
    public static class StrFunc
    {
        /// <summary>
        /// Determines whether the specified string is empty; null is also treated as empty.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <param name="isTrim">Whether to trim leading and trailing whitespace before checking.</param>
        public static bool IsEmpty(string s, bool isTrim = true)
        {
            if (BaseFunc.IsNullOrDBNull(s))
                return true;
            if (isTrim)
                s = s.Trim();
            return (s == string.Empty);
        }

        /// <summary>
        /// Casts the value to a string, then determines whether it is empty; null is also treated as empty.
        /// </summary>
        /// <param name="s">The value to check.</param>
        public static bool IsEmpty(object s)
        {
            return IsEmpty(BaseFunc.CStr(s));
        }

        /// <summary>
        /// Determines whether the specified string is not empty.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <param name="isTrim">Whether to trim leading and trailing whitespace before checking.</param>
        public static bool IsNotEmpty(string s, bool isTrim = true)
        {
            return !IsEmpty(s, isTrim);
        }

        /// <summary>
        /// Casts the value to a string, then determines whether it is not empty; null is also treated as empty.
        /// </summary>
        /// <param name="s">The value to check.</param>
        public static bool IsNotEmpty(object s)
        {
            return IsNotEmpty(BaseFunc.CStr(s));
        }

        /// <summary>
        /// Formats a string using the specified arguments.
        /// </summary>
        /// <param name="s">The format string.</param>
        /// <param name="args">An array of arguments.</param>
        public static string Format(string s, params object[] args)
        {
            return string.Format(s, args);
        }

        /// <summary>
        /// Formats a string using values from a data row.
        /// </summary>
        /// <param name="s">The format string.</param>
        /// <param name="row">The data row.</param>
        /// <param name="args">An array of column name arguments.</param>
        public static string Format(string s, DataRow row, params string[] args)
        {
            object[] oValues;

            if (args == null) { return s; }

            oValues = new object[args.Length];
            for (int N1 = 0; N1 < args.Length; N1++)
                oValues[N1] = row[args[N1]];
            return Format(s, oValues);
        }

        /// <summary>
        /// Determines whether two strings are equal.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <param name="isTrim">Whether to trim leading and trailing whitespace before comparing.</param>
        /// <param name="ignoreCase">Whether to ignore case.</param>
        public static bool IsEquals(string s1, string s2, bool isTrim = false, bool ignoreCase = true)
        {
            if (s1 == null)
                return (s2 == null);
            if (s2 == null)
                return false;

            if (isTrim)
            {
                s1 = s1.Trim();
                s2 = s2.Trim();
            }

            if (ignoreCase)
                return s1.Equals(s2, StringComparison.CurrentCultureIgnoreCase);
            else
                return s1.Equals(s2, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Determines whether the string equals any member of the comparison string array.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <param name="values">The array of strings to compare against.</param>
        public static bool IsEqualsOr(string s, params string[] values)
        {
            foreach (string value in values)
            {
                if (IsEquals(s, value))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Converts a string to uppercase.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        public static string ToUpper(string s)
        {
            if (s == null)
                return string.Empty;
            else
                return s.ToUpper();
        }

        /// <summary>
        /// Converts a string to lowercase.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        public static string ToLower(string s)
        {
            if (s == null)
                return string.Empty;
            else
                return s.ToLower();
        }

        /// <summary>
        /// Replaces occurrences of a substring within a string.
        /// </summary>
        /// <param name="s">The string to process.</param>
        /// <param name="search">The substring to search for.</param>
        /// <param name="replacement">The replacement substring.</param>
        /// <param name="ignoreCase">Whether to ignore case.</param>
        public static string Replace(string s, string search, string replacement, bool ignoreCase = true)
        {
            RegexOptions oOptions;

            // Return empty string directly if the input is empty
            if (IsEmpty(s)) { return string.Empty; }

            oOptions = (ignoreCase) ? RegexOptions.IgnoreCase : RegexOptions.None;
            return Regex.Replace(s, Regex.Escape(search), replacement, oOptions);
        }

        /// <summary>
        /// Splits a string into an array using the specified delimiter.
        /// </summary>
        /// <param name="s">The string to split.</param>
        /// <param name="delimiter">The delimiter.</param>
        public static string[] Split(string s, string delimiter)
        {
            if (IsEmpty(s))
                return new string[] { };
            else
                return s.Split(new string[] { delimiter }, StringSplitOptions.None);
        }

        /// <summary>
        /// Splits a string into an array using newline characters as delimiters.
        /// </summary>
        /// <param name="s">The string to split.</param>
        public static string[] SplitNewLine(string s)
        {
            if (StrFunc.IsEmpty(s))
                return new string[0];
            // Replace \r with empty string first, then split using \n as the delimiter
            return s.Replace("\r", "").Split(new char[] { '\n' });
        }

        /// <summary>
        /// Searches for the delimiter from the left and splits the string into left and right parts.
        /// </summary>
        /// <param name="s">The string to split.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="left">The output left portion of the string.</param>
        /// <param name="right">The output right portion of the string.</param>
        public static void SplitLeft(string s, string delimiter, out string left, out string right)
        {
            int iPos;

            iPos = Pos(s, delimiter);
            left = Left(s, iPos);
            right = Substring(s, iPos + 1);
        }

        /// <summary>
        /// Searches for the delimiter from the right and splits the string into left and right parts.
        /// </summary>
        /// <param name="s">The string to split.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="left">The output left portion of the string.</param>
        /// <param name="right">The output right portion of the string.</param>
        public static void SplitRight(string s, string delimiter, out string left, out string right)
        {
            int iPos;

            iPos = PosRev(s, delimiter);
            left = Left(s, iPos);
            right = Substring(s, iPos + 1);
        }

        /// <summary>
        /// Appends a string and delimiter to the buffer.
        /// </summary>
        /// <param name="buffer">The string buffer.</param>
        /// <param name="s">The new string to append.</param>
        /// <param name="delimiter">The delimiter.</param>
        public static void Append(StringBuilder buffer, string s, string delimiter)
        {
            if (buffer.Length > 0)
                buffer.Append(delimiter);
            buffer.Append(s);
        }

        /// <summary>
        /// Merges two strings using the specified delimiter.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <param name="delimiter">The delimiter.</param>
        public static string Merge(string s1, string s2, string delimiter)
        {
            if (IsNotEmpty(s1))
                s1 += delimiter;
            return s1 + s2;
        }

        /// <summary>
        /// Appends a delimiter and string to the buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="s">The string to append.</param>
        /// <param name="delimiter">The delimiter.</param>
        public static void Merge(StringBuilder buffer, string s, string delimiter)
        {
            if (buffer.Length > 0)
                buffer.Append(delimiter);
            buffer.Append(s);
        }

        /// <summary>
        /// Gets a substring of the specified length from the left side of the string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="length">The length.</param>
        public static string Left(string s, int length)
        {
            if (IsEmpty(s) || (length <= 0))
                return string.Empty;
            else
                return s.Substring(0, length);
        }

        /// <summary>
        /// Gets a substring of the specified length from the right side of the string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="length">The length.</param>
        public static string Right(string s, int length)
        {
            int iStartIndex;

            if (IsEmpty(s) || (length <= 0)) { return string.Empty; }

            // Calculate the starting position for extraction
            iStartIndex = s.Length - length;
            // Extract the substring from the calculated start index
            return Substring(s, iStartIndex);
        }

        /// <summary>
        /// Determines whether the string starts with the specified value.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The value to check against.</param>
        public static bool LeftWith(string s, string value)
        {
            if (IsEmpty(s))
                return false;
            else
                return s.StartsWith(value, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Determines whether the string ends with the specified value.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The value to check against.</param>
        public static bool RightWith(string s, string value)
        {
            if (IsEmpty(s))
                return false;
            else
                return s.EndsWith(value, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Removes the specified number of characters from the left side of the string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="length">The number of characters to remove.</param>
        public static string LeftCut(string s, int length)
        {
            return Substring(s, length);
        }

        /// <summary>
        /// Removes the specified prefix string from the left side if present.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The prefix string to remove.</param>
        public static string LeftCut(string s, string value)
        {
            if (LeftWith(s, value))
                return LeftCut(s, value.Length);
            else
                return s;
        }

        /// <summary>
        /// Removes the specified number of characters from the right side of the string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="length">The number of characters to remove.</param>
        public static string RightCut(string s, int length)
        {
            return Left(s, s.Length - length);
        }

        /// <summary>
        /// Removes the specified suffix string from the right side if present.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The suffix string to remove.</param>
        public static string RightCut(string s, string value)
        {
            if (RightWith(s, value))
                return RightCut(s, value.Length);
            else
                return s;
        }

        /// <summary>
        /// Removes the specified strings from both the left and right sides of the string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="leftValue">The left-side string to remove.</param>
        /// <param name="rightValue">The right-side string to remove.</param>
        public static string LeftRightCut(string s, string leftValue, string rightValue)
        {
            string sValue;

            sValue = LeftCut(s, leftValue);
            sValue = RightCut(sValue, rightValue);
            return sValue;
        }

        /// <summary>
        /// Extracts a substring starting from the specified index.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="startIndex">The zero-based start index; if less than zero, it is forced to zero.</param>
        public static string Substring(string s, int startIndex)
        {
            if (IsEmpty(s)) { return string.Empty; }

            // Calculate the start index; force to zero if less than zero
            if (startIndex < 0) { startIndex = 0; }
            return s.Substring(startIndex);
        }

        /// <summary>
        /// Extracts a substring of the specified length starting from the specified index.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="startIndex">The zero-based start index; if less than zero, it is forced to zero.</param>
        /// <param name="length">The length of the substring to extract.</param>
        public static string Substring(string s, int startIndex, int length)
        {
            if (IsEmpty(s) || (length <= 0)) { return string.Empty; }

            // Calculate the start index; force to zero if less than zero
            if (startIndex < 0) { startIndex = 0; }

            // If the extraction length exceeds the range, return the substring from the start index, ignoring the length
            if ((startIndex + length) > s.Length)
                return s.Substring(startIndex);
            else
                return s.Substring(startIndex, length);
        }

        /// <summary>
        /// Finds the starting position of a substring within the string; returns -1 if not found.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="subString">The substring to find.</param>
        public static int Pos(string s, string subString)
        {
            if (IsEmpty(s))
                return -1;
            // Case-insensitive: convert to uppercase before finding the substring position
            return ToUpper(s).IndexOf(ToUpper(subString));
        }

        /// <summary>
        /// Finds the position of a substring by searching from the right; returns -1 if not found.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="subString">The substring to find.</param>
        public static int PosRev(string s, string subString)
        {
            if (IsEmpty(s))
                return -1;
            else
                return ToUpper(s).LastIndexOf(ToUpper(subString));
        }

        /// <summary>
        /// Determines whether the string contains the specified substring.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="subString">The substring to look for.</param>
        public static bool Contains(string s, string subString)
        {
            if (Pos(s, subString) == -1)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Trims leading and trailing whitespace from the string.
        /// </summary>
        /// <param name="s">The string to trim.</param>
        public static string Trim(string s)
        {
            // Also removes invisible characters: ZERO WIDTH SPACE (U+200B) and ZERO WIDTH NO-BREAK SPACE (U+FEFF)
            // http://blog.miniasp.com/post/2014/01/15/C-Sharp-String-Trim-ZWSP-Zero-width-space.aspx
            if (s == null)
                return string.Empty;
            else
                return s.Trim().Trim(new char[] { '\uFEFF', '\u200B' });
        }

        /// <summary>
        /// Gets the length of the string.
        /// </summary>
        /// <param name="s">The string.</param>
        public static int Length(string s)
        {
            if (IsEmpty(s))
                return 0;
            else
                return s.Length;
        }

        /// <summary>
        /// Left-pads the string to the specified length using the specified character.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="length">The target length.</param>
        /// <param name="paddingChar">The padding character.</param>
        public static string PadLeft(string s, int length, char paddingChar)
        {
            return s.PadLeft(length, paddingChar);
        }

        /// <summary>
        /// Returns a string consisting of the specified character repeated the specified number of times.
        /// </summary>
        /// <param name="number">The number of repetitions.</param>
        /// <param name="character">The character to repeat.</param>
        public static string Dup(int number, char character)
        {
            return PadLeft(string.Empty, number, character);
        }

        /// <summary>
        /// Mimics VB's LikeString method, supporting *, ?, # wildcards for string matching.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="pattern">The match pattern using VB Like syntax.</param>
        /// <param name="compareOption">The comparison option (e.g. IgnoreCase).</param>
        /// <returns>True if the source matches the specified pattern; otherwise, false.</returns>
        public static bool Like(string source, string pattern, CompareOptions compareOption = CompareOptions.IgnoreCase)
        {
            if (source == null || pattern == null)
                return false;

            // Escape regex, then restore wildcards to Regex syntax
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace(@"\*", ".*")     // *: any sequence of characters
                .Replace(@"\?", ".")      // ?: any single character
                .Replace(@"\#", "[0-9]")  // #: any single digit
                + "$";

            var options = RegexOptions.Compiled;
            if (compareOption.HasFlag(CompareOptions.IgnoreCase))
                options |= RegexOptions.IgnoreCase;

            return Regex.IsMatch(source, regexPattern, options);
        }

        /// <summary>
        /// Gets the next sequential ID (supports base 2 to 36).
        /// </summary>
        /// <param name="value">The current ID.</param>
        /// <param name="numberBase">The base for the sequential ID (2-36).</param>
        /// <returns>The next sequential ID.</returns>
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
        /// <returns>The next sequential ID.</returns>
        public static string GetNextId(string value, string baseValues)
        {
            if (string.IsNullOrEmpty(baseValues))
                throw new ArgumentException("Base values must not be null or empty.", nameof(baseValues));

            var digits = baseValues.ToCharArray();
            var baseLength = digits.Length;
            var current = StrFunc.Trim(value).ToCharArray();

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
    }
}
