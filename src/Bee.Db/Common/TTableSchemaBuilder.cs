using Bee.Base;
using Bee.Cache;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料表結構產生器。
    /// </summary>
    public class TTableSchemaBuilder
    {
        private readonly string _DatabaseID = string.Empty;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        public TTableSchemaBuilder(string databaseID)
        {
            _DatabaseID = databaseID;
        }

        #endregion

        /// <summary>
        /// 資料庫編號。
        /// </summary>
        public string DatabaseID
        {
            get { return _DatabaseID; }
        }

        /// <summary>
        /// 執行資料表結構比對。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public TDbTable Compare(string dbName, string tableName)
        {
            // 實際的資料表結構
            var helper = new TSqlDbTableHelper(this.DatabaseID);
            var realTable = helper.CreateDbTable(tableName);
            // 定義的資料表結構
            var defineTable = CacheFunc.GetDbTable(dbName, tableName);
            // 執行比對，並傳回比對後產生的資料表結構
            var comparer = new TTableSchemaComparer(defineTable, realTable);
            return comparer.Compare();
        }

        /// <summary>
        /// 進行資料表結構比對，產生資料庫命令。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public string GetCommandText(string dbName, string tableName)
        {
            // 執行資料表結構比對後，回傳要升級的資料表結構
            var dbTable = this.Compare(dbName, tableName);
            if (dbTable.UpgradeAction != EDbUpgradeAction.None)
            {
                var builder = new TSqlCreateTableCommandBuilder();
                return builder.GetCommandText(dbTable);
            }
            return string.Empty;
        }

        /// <summary>
        /// 執行資料表結構比對並進行升級。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        /// <remarks>結構有差異執行升級傳回 true，反之傳回 false。</remarks>
        public bool Execute(string dbName, string tableName)
        {
            string sql = this.GetCommandText(dbName, tableName);
            if (StrFunc.IsNotEmpty(sql))
            {
                SysDb.ExecuteNonQuery(this.DatabaseID, sql);
                return true;
            }
            return false;
        }
    }
}
