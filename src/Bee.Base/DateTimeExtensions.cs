namespace Bee.Base
{
    /// <summary>
    /// Extension methods for <see cref="DateTime"/>.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Determines whether the specified date/time value is empty
        /// (before SQL Server's minimum datetime value 1753-01-01).
        /// </summary>
        /// <param name="dateValue">The date/time value to check.</param>
        public static bool IsEmpty(this DateTime dateValue)
        {
            // SQL Server's datetime minimum is 1753-01-01; values earlier are treated as empty
            return dateValue < new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Gets the first day of the year and month of the specified date.
        /// </summary>
        /// <param name="dateValue">The date value.</param>
        public static DateTime GetYearMonth(this DateTime dateValue)
        {
            return new DateTime(dateValue.Year, dateValue.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        }
    }
}
