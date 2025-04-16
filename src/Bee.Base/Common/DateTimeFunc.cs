using System;

namespace Bee.Base
{
    /// <summary>
    /// 日期時間函式庫。
    /// </summary>
    public static class DateTimeFunc
    {
        /// <summary>
        /// 判斷日期時間值是否為未設定。
        /// </summary>
        /// <param name="dateValue">日期時間值。</param>
        public static bool IsEmpty(DateTime dateValue)
        {
            // SQL 資料庫的 DateTime 最小值為 1753/1/1，小於此值視為空值
            return dateValue < new DateTime(1753, 1, 1);
        }

        /// <summary>
        /// 判斷傳入值是否為日期格式。
        /// </summary>
        /// <param name="value">要判斷的值。</param>
        public static bool IsDate(object value)
        {
            if (value is DateTime) { return true; }
            return DateTime.TryParse(BaseFunc.CStr(value), out _);
        }

        /// <summary>
        /// 日期時間值格式化。
        /// </summary>
        /// <param name="dateValue">日期時間值。</param>
        /// <param name="format">格式化字串。</param>
        public static string Format(DateTime dateValue, string format)
        {
            return dateValue.ToString(format);
        }

        /// <summary>
        /// 取得傳入日期的年月。
        /// </summary>
        /// <param name="value">日期值。</param>
        public static DateTime GetYearMonth(DateTime value)
        {
            return new DateTime(value.Year, value.Month, 1);
        }
    }
}
