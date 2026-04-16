using System.Data;

namespace Bee.Base.Data
{
    /// <summary>
    /// Extension methods for <see cref="DataView"/>.
    /// </summary>
    public static class DataViewExtensions
    {
        /// <summary>
        /// Deletes all rows in the view.
        /// </summary>
        /// <param name="dataView">The data view.</param>
        /// <param name="acceptChanges">Whether to call <see cref="DataTable.AcceptChanges"/> after deletion.</param>
        public static void DeleteRows(this DataView dataView, bool acceptChanges)
        {
            for (int N1 = dataView.Count - 1; N1 >= 0; N1 += -1)
                dataView.Delete(N1);

            if (acceptChanges)
                dataView.Table?.AcceptChanges();
        }

        /// <summary>
        /// Determines whether the view's underlying table contains the specified column.
        /// </summary>
        /// <param name="dataView">The data view.</param>
        /// <param name="fieldName">The column name to check.</param>
        public static bool HasField(this DataView dataView, string fieldName)
        {
            return dataView.Table?.HasField(fieldName) ?? false;
        }

        /// <summary>
        /// Determines whether the view contains no rows.
        /// </summary>
        /// <param name="dataView">The data view to check.</param>
        public static bool IsEmpty(this DataView dataView)
        {
            // A null view or a view with zero rows is considered empty
            return (dataView == null || (dataView.Count == 0));
        }
    }
}
