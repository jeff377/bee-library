using System.Data;

namespace Bee.Base
{
    /// <summary>
    /// 提供 DataTable 比對的靜態方法，用來判斷兩個資料表是否在資料列狀態與欄位值上完全一致。
    /// </summary>
    public static class DataTableComparer
    {
        /// <summary>
        /// 比對兩個 DataTable 是否在 RowState 狀態與欄位值上完全相同。
        /// </summary>
        /// <param name="dt1">第一個 DataTable。</param>
        /// <param name="dt2">第二個 DataTable。</param>
        /// <returns>若兩者完全一致，回傳 true，否則回傳 false。</returns>
        public static bool IsEqual(DataTable dt1, DataTable dt2)
        {
            if (dt1 == null || dt2 == null) return false;
            if (dt1.TableName != dt2.TableName) return false;
            if (dt1.Columns.Count != dt2.Columns.Count) return false;
            if (dt1.Rows.Count != dt2.Rows.Count) return false;

            // 比對欄位名稱與資料型別
            for (int i = 0; i < dt1.Columns.Count; i++)
            {
                var col1 = dt1.Columns[i];
                var col2 = dt2.Columns[i];
                if (col1.ColumnName != col2.ColumnName || col1.DataType != col2.DataType)
                    return false;
            }

            // 比對每一筆資料列的狀態與資料內容
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
                        // 比對刪除列的原始值
                        var v1 = row1[colName, DataRowVersion.Original];
                        var v2 = row2[colName, DataRowVersion.Original];
                        if (!Equals(v1, v2))
                            return false;
                    }
                    else if (row1.RowState == DataRowState.Modified)
                    {
                        // 比對修改列的原始值與目前值
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
                        // 比對新增或未變更列的目前值
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

