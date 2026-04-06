namespace Bee.Base
{
    /// <summary>
    /// Specifies the interval used in date/time calculations.
    /// </summary>
    public enum DateInterval
    {
        /// <summary>
        /// Year.
        /// </summary>
        Year = 0,
        /// <summary>
        /// Quarter (1 to 4).
        /// </summary>
        Quarter = 1,
        /// <summary>
        /// Month (1 to 12).
        /// </summary>
        Month = 2,
        /// <summary>
        /// Day of year (1 to 366).
        /// </summary>
        DayOfYear = 3,
        /// <summary>
        /// Day of month (1 to 31).
        /// </summary>
        Day = 4,
        /// <summary>
        /// Week of year (1 to 53).
        /// </summary>
        WeekOfYear = 5,
        /// <summary>
        /// Day of week (1 to 7).
        /// </summary>
        Weekday = 6,
        /// <summary>
        /// Hour (1 to 24).
        /// </summary>
        Hour = 7,
        /// <summary>
        /// Minute (1 to 60).
        /// </summary>
        Minute = 8,
        /// <summary>
        /// Second (1 to 60).
        /// </summary>
        Second = 9
    }
}
