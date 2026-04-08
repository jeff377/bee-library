using System.Data;

namespace Bee.Core.Data
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
            if (dt1.TableName != dt2.TableName) return false;
            if (dt1.Columns.Count != dt2.Columns.Count) return false;
            if (dt1.Rows.Count != dt2.Rows.Count) return false;

            // Compare column names and data types
            for (int i = 0; i < dt1.Columns.Count; i++)
            {
                var col1 = dt1.Columns[i];
                var col2 = dt2.Columns[i];
                if (col1.ColumnName != col2.ColumnName || col1.DataType != col2.DataType)
                    return false;
            }

            // Compare row state and column values for each row
            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                var row1 = dt1.Rows[i];
                var row2 = dt2.Rows[i];

                if (row1.RowState != row2.RowState)
                    return false;

                foreach (DataColumn col in dt1.Columns)
                {
                    var colName = col.ColumnName;

                    if (row1.RowState == DataRowState.Deleted)
                    {
                        // Compare original values for deleted rows
                        var v1 = row1[colName, DataRowVersion.Original];
                        var v2 = row2[colName, DataRowVersion.Original];
                        if (!Equals(v1, v2))
                            return false;
                    }
                    else if (row1.RowState == DataRowState.Modified)
                    {
                        // Compare both current and original values for modified rows
                        var curr1 = row1[colName, DataRowVersion.Current];
                        var curr2 = row2[colName, DataRowVersion.Current];
                        if (!Equals(curr1, curr2))
                            return false;

                        var orig1 = row1[colName, DataRowVersion.Original];
                        var orig2 = row2[colName, DataRowVersion.Original];
                        if (!Equals(orig1, orig2))
                            return false;
                    }
                    else
                    {
                        // Compare current values for added or unchanged rows
                        var v1 = row1[colName];
                        var v2 = row2[colName];
                        if (!Equals(v1, v2))
                            return false;
                    }
                }
            }

            return true;
        }
    }
}

