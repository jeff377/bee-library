using System;
using System.Data;

namespace Bee.Core.Data
{
    /// <summary>
    /// Extension methods for <see cref="DataRowView"/>.
    /// </summary>
    public static class DataRowViewExtensions
    {
        /// <summary>
        /// Gets the value of the specified column and converts it to the target type.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="rowView">The data row view.</param>
        /// <param name="columnName">The column name.</param>
        /// <returns>The column value converted to <typeparamref name="T"/>, or <c>default(T)</c> if the value is <see cref="DBNull"/>.</returns>
        /// <exception cref="InvalidOperationException">The column does not exist or the conversion fails.</exception>
        public static T GetFieldValue<T>(this DataRowView rowView, string columnName)
        {
            return rowView.Row.GetFieldValue<T>(columnName);
        }

        /// <summary>
        /// Gets the value of the specified column and converts it to the target type, returning a default value if the column does not exist.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="rowView">The data row view.</param>
        /// <param name="columnName">The column name.</param>
        /// <param name="defaultValue">The default value to return if the column does not exist.</param>
        /// <returns>The column value converted to <typeparamref name="T"/>, or <c>default(T)</c> if the value is <see cref="DBNull"/>.</returns>
        public static T GetFieldValue<T>(this DataRowView rowView, string columnName, T defaultValue)
        {
            return rowView.Row.GetFieldValue<T>(columnName, defaultValue);
        }
    }
}
