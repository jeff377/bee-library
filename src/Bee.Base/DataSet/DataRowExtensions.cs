using System;
using System.Data;

namespace Bee.Base
{
    /// <summary>
    /// DataRow 的擴充方法。
    /// </summary>
    public static class DataRowExtensions
    {
        /// <summary>
        /// 取得指定欄位的值並轉換為指定型別。
        /// </summary>
        /// <typeparam name="T">目標型別</typeparam>
        /// <param name="row">資料列</param>
        /// <param name="columnName">欄位名稱</param>
        /// <returns>指定型別的欄位值，若為 DBNull 則回傳 default(T)</returns>
        /// <exception cref="InvalidOperationException">欄位不存在或轉換失敗</exception>
        public static T GetFieldValue<T>(this DataRow row, string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException(nameof(columnName), "Parameter cannot be null or empty.");

            if (!row.Table.Columns.Contains(columnName))
                throw new InvalidOperationException($"Unable to get field value: Column '{columnName}' does not exist.");

            object value = row[columnName];

            if (value == DBNull.Value) { return default(T); }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to convert field '{columnName}' to type {typeof(T).Name}: {ex.Message}", ex
                );
            }
        }

        /// <summary>
        /// 取得指定欄位的值並轉換為指定型別，若欄位不存在則以預設值取代。
        /// </summary>
        /// <typeparam name="T">目標型別</typeparam>
        /// <param name="row">資料列</param>
        /// <param name="columnName">欄位名稱</param>
        /// <param name="defaultValue">預設值。</param>
        /// <returns>指定型別的欄位值，若為 DBNull 則回傳 default(T)</returns>
        public static T GetFieldValue<T>(this DataRow row, string columnName, T defaultValue)
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException(nameof(columnName), "Parameter cannot be null or empty.");

            if (!row.Table.Columns.Contains(columnName))
                return defaultValue;

            object value = row[columnName];

            if (value == DBNull.Value) { return default(T); }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to convert field '{columnName}' to type {typeof(T).Name}: {ex.Message}", ex
                );
            }
        }
    }
}
