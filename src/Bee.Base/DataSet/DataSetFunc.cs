using System;
using System.Data;

namespace Bee.Base
{
    /// <summary>
    /// DataSet 相關函式庫。
    /// </summary>
    public static class DataSetFunc
    {
        /// <summary>
        /// 判斷資料表是否無資料。
        /// </summary>
        /// <param name="dataTable">要判斷的資料表。</param>
        public static bool IsEmpty(DataTable dataTable)
        {
            // 資料表為 Null 或資料列數為零，皆視為無資料
            return (dataTable == null || (dataTable.Rows.Count == 0));
        }

        /// <summary>
        /// 判斷檢視表是否無資料。
        /// </summary>
        /// <param name="dataView">要判斷的檢視表。</param>
        public static bool IsEmpty(DataView dataView)
        {
            //檢視表為 Null 或資料列數為零，皆視為無資料
            return (dataView == null || (dataView.Count == 0));
        }

        /// <summary>
        /// 判斷資料集是否無資料。
        /// </summary>
        /// <param name="dataSet">要判斷的資料集。</param>
        public static bool IsEmpty(DataSet dataSet)
        {
            // 資料集為 null 或無資料表，皆視為無資料
            if (dataSet == null || (dataSet.Tables.Count == 0)) { return true; }
            // 主檔資料表無資料時，也視為無資料
            var table = GetMasterTable(dataSet);
            if (table != null)
                return IsEmpty(table);
            return false;
        }

        /// <summary>
        /// 建立資料集。
        /// </summary>
        /// <param name="datasetName">資料集名稱。</param>
        public static DataSet CreateDataSet(string datasetName)
        {
            DataSet oDataSet;

            oDataSet = new DataSet(datasetName);
            oDataSet.RemotingFormat = SerializationFormat.Binary;
            return oDataSet;
        }

        /// <summary>
        /// 建立資料集。
        /// </summary>
        public static DataSet CreateDataSet()
        {
            return CreateDataSet("DataSet");
        }

        /// <summary>
        /// 取得主檔資料表。
        /// </summary>
        /// <param name="dataSet">資料集。</param>
        public static DataTable GetMasterTable(DataSet dataSet)
        {
            if (dataSet == null) { return null; }
            if (StrFunc.IsEmpty(dataSet.DataSetName)) { return null; }
            if (!dataSet.Tables.Contains(dataSet.DataSetName)) { return null; }
            // 主檔資料表 TableName 等於 DataSetName，視為主檔資料表
            return dataSet.Tables[dataSet.DataSetName];
        }

        /// <summary>
        /// 取得主檔資料列。
        /// </summary>
        /// <param name="dataSet">資料集。</param>
        public static DataRow GetMasterRow(DataSet dataSet)
        {
            var table = GetMasterTable(dataSet);
            if (IsEmpty(table)) { return null; }
            return table.Rows[0];
        }

        /// <summary>
        /// 建立資料表。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        public static DataTable CreateDataTable(string tableName)
        {
            var table = new DataTable(tableName);
            table.RemotingFormat = SerializationFormat.Binary;
            return table;
        }

        /// <summary>
        /// 建立資料表。
        /// </summary>
        public static DataTable CreateDataTable()
        {
            return CreateDataTable("DataTable");
        }

        /// <summary>
        /// 複製資料表，只保留指定欄位。
        /// </summary>
        /// <param name="source">來源資料表</param>
        /// <param name="fieldNames">保留欄位名稱陣列。</param>
        public static DataTable CopyDataTable(DataTable source, string[] fieldNames)
        {
            // 複製來源資料及結構
            var table = source.Copy();
            // 欄位名稱先轉為大寫
            for (int N1 = 0; N1 < fieldNames.Length; N1++)
                fieldNames[N1] = StrFunc.ToUpper(fieldNames[N1]);
            // 去除不需要的欄位
            for (int N1 = table.Columns.Count - 1; N1 >= 0; N1--)
            {
                string fieldName = table.Columns[N1].ColumnName.ToUpper();
                if (Array.IndexOf(fieldNames, fieldName) == -1)
                    table.Columns.Remove(fieldName);
            }
            // 重新設定欄位順序
            for (int N1 = 0; N1 < fieldNames.Length - 1; N1++)
            {
                string fieldName = fieldNames[N1];
                table.Columns[fieldName].SetOrdinal(N1);
            }
            // 回傳處理後的資料表
            return table;
        }

        /// <summary>
        /// 變更為大寫欄位名稱。
        /// </summary>
        /// <param name="dataTable">資料表。</param>
        public static void UpperColumnName(DataTable dataTable)
        {
            foreach (DataColumn column in dataTable.Columns)
                column.ColumnName = column.ColumnName.ToUpper();
        }

        /// <summary>
        /// 判斷是否有指定欄位。
        /// </summary>
        /// <param name="dataTable">資料表。</param>
        /// <param name="fieldName">欄位名稱。</param>
        public static bool HasField(DataTable dataTable, string fieldName)
        {
            return dataTable.Columns.Contains(fieldName);
        }

        /// <summary>
        /// 判斷是否有指定欄位。
        /// </summary>
        /// <param name="dataView">檢視資料表。</param>
        /// <param name="fieldName">欄位名稱。</param>
        public static bool HasField(DataView dataView, string fieldName)
        {
            return HasField(dataView.Table, fieldName);
        }

        /// <summary>
        /// 判斷是否有指定欄位。
        /// </summary>
        /// <param name="row">資料列。</param>
        /// <param name="fieldName">欄位名稱。</param>
        public static bool HasField(DataRow row, string fieldName)
        {
            return HasField(row.Table, fieldName);
        }

        /// <summary>
        /// 判斷是否有指定欄位。
        /// </summary>
        /// <param name="row">檢視資料列。</param>
        /// <param name="fieldName">欄位名稱。</param>
        public static bool HasField(DataRowView row, string fieldName)
        {
            return HasField(row.Row, fieldName);
        }

        /// <summary>
        /// 依欄位資料型別取得預設值。
        /// </summary>
        /// <param name="dbType">欄位資料型別。</param>
        public static object GetDefaultValue(FieldDbType dbType)
        {
            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return string.Empty;
                case FieldDbType.Boolean:
                    return false;
                case FieldDbType.Integer:
                case FieldDbType.Double:
                case FieldDbType.Currency:
                    return 0;
                case FieldDbType.Date:
                    return DateTime.Today;
                case FieldDbType.DateTime:
                    return DateTime.Now;
                case FieldDbType.Guid:
                    return Guid.Empty;
                default:
                    return DBNull.Value;
            }
        }

        /// <summary>
        /// 刪除檢視表中所有的資料列。
        /// </summary>
        /// <param name="dataView">檢視表。</param>
        /// <param name="acceptChanges">是否同意變更。</param>
        public static void DeleteRows(DataView dataView, bool acceptChanges)
        {
            for (int N1 = dataView.Count - 1; N1 >= 0; N1 += -1)
                dataView.Delete(N1);

            if (acceptChanges)
                dataView.Table.AcceptChanges();
        }

        /// <summary>
        /// 刪除檢視表中所有的資料列。
        /// </summary>
        /// <param name="dataView">檢視表。</param>
        public static void DeleteRows(DataView dataView)
        {
            DeleteRows(dataView, false);
        }

        /// <summary>
        /// 刪除資料表中所有的資料列。
        /// </summary>
        /// <param name="dataTable">資料表。</param>
        /// <param name="acceptChanges">是否同意變更。</param>
        public static void DeleteRows(DataTable dataTable, bool acceptChanges)
        {
            DeleteRows(dataTable.DefaultView, acceptChanges);
        }
    }
}
