using System;
using System.Data;

namespace Bee.Base.Data
{
    /// <summary>
    /// Extension methods for <see cref="DataRow"/>.
    /// </summary>
    public static class DataRowExtensions
    {
        /// <summary>
        /// Gets the value of the specified column and converts it to the target type.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="row">The data row.</param>
        /// <param name="columnName">The column name.</param>
        /// <returns>The column value converted to <typeparamref name="T"/>, or <c>default(T)</c> if the value is <see cref="DBNull"/>.</returns>
        /// <exception cref="InvalidOperationException">The column does not exist or the conversion fails.</exception>
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
        /// Gets the value of the specified column and converts it to the target type, returning a default value if the column does not exist.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="row">The data row.</param>
        /// <param name="columnName">The column name.</param>
        /// <param name="defaultValue">The default value to return if the column does not exist.</param>
        /// <returns>The column value converted to <typeparamref name="T"/>, or <c>default(T)</c> if the value is <see cref="DBNull"/>.</returns>
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
