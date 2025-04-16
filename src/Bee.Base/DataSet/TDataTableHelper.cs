using System;
using System.Data;

namespace Bee.Base
{
    /// <summary>
    /// DataTable 操作輔助類別。
    /// </summary>
    public class TDataTableHelper
    {
        private DataTable _DataTable = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="dataTable">資料表。</param>
        public TDataTableHelper(DataTable dataTable)
        {
            _DataTable = dataTable;
        }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        public TDataTableHelper(string tableName)
        {
            _DataTable = DataSetFunc.CreateDataTable(tableName);
        }

        #endregion

        /// <summary>
        /// 資料表。
        /// </summary>
        public DataTable DataTable
        {
            get { return _DataTable; }
        }

        /// <summary>
        /// 建立欄位並加入資料表。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="dbType">欄位資料型別。</param>
        public DataColumn AddColumn(string fieldName, EFieldDbType dbType)
        {
            return this.DataTable.AddColumn(fieldName, dbType);
        }

        /// <summary>
        /// 建立欄位並加入資料表。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="dbType">欄位資料型別。</param>
        /// <param name="defaultValue">預設值。</param>
        public DataColumn AddColumn(string fieldName, EFieldDbType dbType, object defaultValue)
        {
            return this.DataTable.AddColumn(fieldName, dbType, defaultValue);
        }

        /// <summary>
        /// 建立欄位並加入資料表中。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">欄位標題。</param>
        /// <param name="dbType">欄位資料型別。</param>
        /// <param name="defaultValue">預設值。</param>
        public DataColumn AddColumn(string fieldName, string caption, EFieldDbType dbType, object defaultValue)
        {
            return this.DataTable.AddColumn(fieldName, caption, dbType, defaultValue);
        }

        /// <summary>
        /// 設定資料表的主索引鍵。
        /// </summary>
        /// <param name="fieldNames">主索引鍵的欄位集合字串(以逗點分隔多個欄位名稱)。</param>
        public void SetPrimaryKey(string fieldNames)
        {
            this.DataTable.SetPrimaryKey(fieldNames);
        }
    }
}
