using System.Data;
using System.Data.Common;
using Bee.Cache;

namespace Bee.Db
{
    /// <summary>
    /// 系統資料庫操作類別。
    /// </summary>
    public static class SysDb
    {
        /// <summary>
        /// 建立資料庫存取物件。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        private static TDbAccess CreateDbAcccess(string databaseID)
        {
            var database = CacheFunc.GetDatabaseItem(databaseID);
            return new TDbAccess(database);
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        public static DataTable ExecuteDataTable(string databaseID, DbCommand command)
        {
            var dbAccess = CreateDbAcccess(databaseID);
            return dbAccess.ExecuteDataTable(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        public static DataTable ExecuteDataTable(string databaseID, string commandText)
        {
            var dbAccess = CreateDbAcccess(databaseID);
            return dbAccess.ExecuteDataTable(commandText);
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        public static int ExecuteNonQuery(string databaseID, DbCommand command)
        {
            var dbAccess = CreateDbAcccess(databaseID);
            return dbAccess.ExecuteNonQuery(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        public static int ExecuteNonQuery(string databaseID, string commandText)
        {
            var dbAccess = CreateDbAcccess(databaseID);
            return dbAccess.ExecuteNonQuery(commandText);
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        public static object ExecuteScalar(string databaseID, DbCommand command)
        {
            TDbAccess oDbAccess;

            oDbAccess = CreateDbAcccess(databaseID);
            return oDbAccess.ExecuteScalar(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        public static object ExecuteScalar(string databaseID, string commandText)
        {
            TDbAccess oDbAccess;

            oDbAccess = CreateDbAcccess(databaseID);
            return oDbAccess.ExecuteScalar(commandText);
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public static DbDataReader ExecuteReader(string databaseID, DbCommand command)
        {
            var dbAccess = CreateDbAcccess(databaseID);
            return dbAccess.ExecuteReader(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public static DbDataReader ExecuteReader(string databaseID, string commandText)
        {
            var dbAccess = CreateDbAcccess(databaseID);
            return dbAccess.ExecuteReader(commandText);
        }

        /// <summary>
        /// 將 DataTable 的異動寫入資料庫。 
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        /// <param name="dataTable">資料表。</param>
        /// <param name="insertCommand">新增命令。</param>
        /// <param name="updateCommand">更新命令。</param>
        /// <param name="deleteCommand">刪除命令。</param>
        public static int UpdateDataTable(string databaseID, DataTable dataTable, DbCommand insertCommand, DbCommand updateCommand, DbCommand deleteCommand)
        {
            TDbAccess oDbAccess;

            oDbAccess = CreateDbAcccess(databaseID);
            return oDbAccess.UpdateDataTable(dataTable, insertCommand, updateCommand, deleteCommand);
        }
    }
}
