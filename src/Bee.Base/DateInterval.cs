namespace Bee.Base
{
    /// <summary>
    /// 時間間隔。
    /// </summary>
    public enum DateInterval
    {
        /// <summary>
        /// 年。
        /// </summary>
        Year = 0,
        /// <summary>
        /// 季 (1 到 4)
        /// </summary>
        Quarter = 1,
        /// <summary>
        /// 月份 (1 到 12)
        /// </summary>
        Month = 2,
        /// <summary>
        /// 年中的日 (1 到 366)
        /// </summary>
        DayOfYear = 3,
        /// <summary>
        /// 月中的日 (1 到 31)
        /// </summary>
        Day = 4,
        /// <summary>
        /// 年中的週 (1 到 53)
        /// </summary>
        WeekOfYear = 5,
        /// <summary>
        /// 星期資訊 (1 到 7)
        /// </summary>
        Weekday = 6,
        /// <summary>
        /// 小時 (1 到 24)
        /// </summary>
        Hour = 7,
        /// <summary>
        /// 分鐘 (1 到 60)
        /// </summary>
        Minute = 8,
        /// <summary>
        /// 秒鐘 (1 到 60)
        /// </summary>
        Second = 9
    }
}
