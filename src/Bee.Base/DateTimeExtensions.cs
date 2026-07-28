namespace Bee.Base
{
    /// <summary>
    /// Extension methods for <see cref="DateTime"/>.
    /// </summary>
    public static class DateTimeExtensions
    {
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
