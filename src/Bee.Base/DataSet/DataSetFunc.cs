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
        /// 建立資料集。
        /// </summary>
        /// <param name="datasetName">資料集名稱。</param>
        public static DataSet CreateDataSet(string datasetName)
        {
            DataSet oDataSet;

            oDataSet = new DataSet(datasetName);
#pragma warning disable SYSLIB0038
            oDataSet.RemotingFormat = SerializationFormat.Binary;
#pragma warning restore SYSLIB0038
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
        /// 建立資料表。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        public static DataTable CreateDataTable(string tableName)
        {
            var table = new DataTable(tableName);
#pragma warning disable SYSLIB0038
            table.RemotingFormat = SerializationFormat.Binary;
#pragma warning restore SYSLIB0038
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
        /// 依欄位資料型別取得預設值。
        /// </summary>
        /// <param name="dbType">欄位資料型別。</param>
        public static object GetDefaultValue(FieldDbType dbType)
        {
            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Memo:
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
    }
}
