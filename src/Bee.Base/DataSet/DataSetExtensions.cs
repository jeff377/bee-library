using System.Data;

namespace Bee.Base
{
    /// <summary>
    /// DataSet 的擴充方法。
    /// </summary>
    public static class DataSetExtensions
    {
        /// <summary>
        /// 取得主檔資料表。
        /// </summary>
        /// <param name="dataSet">資料集。</param>
        public static DataTable GetMasterTable(this DataSet dataSet)
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
        public static DataRow GetMasterRow(this DataSet dataSet)
        {
            var table = GetMasterTable(dataSet);
            if (table.IsEmpty()) { return null; }
            return table.Rows[0];
        }

        /// <summary>
        /// 判斷資料集是否無資料。
        /// </summary>
        /// <param name="dataSet">要判斷的資料集。</param>
        public static bool IsEmpty(this DataSet dataSet)
        {
            // 資料集為 null 或無資料表，皆視為無資料
            if (dataSet == null || (dataSet.Tables.Count == 0)) { return true; }
            // 主檔資料表無資料時，也視為無資料
            var table = GetMasterTable(dataSet);
            return table.IsEmpty();
        }
    }
}
