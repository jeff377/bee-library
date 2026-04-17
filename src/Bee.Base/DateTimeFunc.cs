using System;
using System.Globalization;

namespace Bee.Base
{
    /// <summary>
    /// Utility library for date and time operations.
    /// </summary>
    public static class DateTimeFunc
    {
        /// <summary>
        /// Determines whether the specified date/time value is empty (not set).
        /// </summary>
        /// <param name="dateValue">The date/time value to check.</param>
        public static bool IsEmpty(DateTime dateValue)
        {
            // The minimum DateTime value in SQL databases is 1753/1/1; values earlier than this are treated as empty
            return dateValue < new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Determines whether the specified value is a valid date.
        /// </summary>
        /// <param name="value">The value to check.</param>
        public static bool IsDate(object value)
        {
            if (value is DateTime) { return true; }
            return DateTime.TryParse(BaseFunc.CStr(value), CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        /// <summary>
        /// Formats a date/time value using the specified format string.
        /// </summary>
        /// <param name="dateValue">The date/time value to format.</param>
        /// <param name="format">The format string.</param>
        public static string Format(DateTime dateValue, string format)
        {
            return dateValue.ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the year and month portion of the specified date value.
        /// </summary>
        /// <param name="value">The date value.</param>
        public static DateTime GetYearMonth(DateTime value)
        {
            return new DateTime(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        }
    }
}
