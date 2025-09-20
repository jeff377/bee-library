using System;
using System.Data;

namespace Bee.Base
{
    /// <summary>
    /// DataRowView 的擴充方法。
    /// </summary>
    public static class DataRowViewExtensions
    {
        /// <summary>
        /// 取得指定欄位的值並轉換為指定型別。
        /// </summary>
        /// <typeparam name="T">目標型別</typeparam>
        /// <param name="rowView">檢視資料列</param>
        /// <param name="columnName">欄位名稱</param>
        /// <returns>指定型別的欄位值，若為 DBNull 則回傳 default(T)</returns>
        /// <exception cref="InvalidOperationException">欄位不存在或轉換失敗</exception>
        public static T GetFieldValue<T>(this DataRowView rowView, string columnName)
        {
            return rowView.Row.GetFieldValue<T>(columnName);
        }

        /// <summary>
        /// 取得指定欄位的值並轉換為指定型別，若欄位不存在則以預設值取代。
        /// </summary>
        /// <typeparam name="T">目標型別</typeparam>
        /// <param name="rowView">資料列</param>
        /// <param name="columnName">欄位名稱</param>
        /// <param name="defaultValue">預設值。</param>
        /// <returns>指定型別的欄位值，若為 DBNull 則回傳 default(T)</returns>
        public static T GetFieldValue<T>(this DataRowView rowView, string columnName, T defaultValue)
        {
            return rowView.Row.GetFieldValue<T>(columnName, defaultValue);
        }
    }
}
