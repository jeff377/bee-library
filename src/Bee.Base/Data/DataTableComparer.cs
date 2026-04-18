using System.Data;

namespace Bee.Base.Data
{
    /// <summary>
    /// Provides static methods for comparing two DataTables by row state and column values.
    /// </summary>
    public static class DataTableComparer
    {
        /// <summary>
        /// Determines whether two DataTables are identical in terms of row state and column values.
        /// </summary>
        /// <param name="dt1">The first DataTable.</param>
        /// <param name="dt2">The second DataTable.</param>
        /// <returns><c>true</c> if both tables are identical; otherwise, <c>false</c>.</returns>
        public static bool IsEqual(DataTable dt1, DataTable dt2)
        {
            if (dt1 == null || dt2 == null) return false;
            if (!AreStructuresEqual(dt1, dt2)) return false;

            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                if (!AreRowsEqual(dt1.Rows[i], dt2.Rows[i], dt1.Columns))
                    return false;
            }

            return true;
        }

        private static bool AreStructuresEqual(DataTable dt1, DataTable dt2)
        {
            if (dt1.TableName != dt2.TableName) return false;
            if (dt1.Columns.Count != dt2.Columns.Count) return false;
            if (dt1.Rows.Count != dt2.Rows.Count) return false;

            for (int i = 0; i < dt1.Columns.Count; i++)
            {
                var col1 = dt1.Columns[i];
                var col2 = dt2.Columns[i];
                if (col1.ColumnName != col2.ColumnName || col1.DataType != col2.DataType)
                    return false;
            }

            return true;
        }

        private static bool AreRowsEqual(DataRow row1, DataRow row2, DataColumnCollection columns)
        {
            if (row1.RowState != row2.RowState) return false;

            foreach (DataColumn col in columns)
            {
                if (!AreColumnValuesEqual(row1, row2, col.ColumnName, row1.RowState))
                    return false;
            }

            return true;
        }

        private static bool AreColumnValuesEqual(DataRow row1, DataRow row2, string colName, DataRowState state)
        {
            if (state == DataRowState.Deleted)
                return Equals(row1[colName, DataRowVersion.Original], row2[colName, DataRowVersion.Original]);

            if (state == DataRowState.Modified)
            {
                return Equals(row1[colName, DataRowVersion.Current], row2[colName, DataRowVersion.Current])
                    && Equals(row1[colName, DataRowVersion.Original], row2[colName, DataRowVersion.Original]);
            }

            return Equals(row1[colName], row2[colName]);
        }
    }
}

