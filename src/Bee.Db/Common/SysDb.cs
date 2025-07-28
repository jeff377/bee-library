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
        /// <param name="databaseId">資料庫編號。</param>
        private static DbAccess CreateDbAcccess(string databaseId)
        {
            var database = CacheFunc.GetDatabaseItem(databaseId);
            return new DbAccess(database);
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        public static DataTable ExecuteDataTable(string databaseId, DbCommand command)
        {
            var dbAccess = CreateDbAcccess(databaseId);
            return dbAccess.ExecuteDataTable(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        public static DataTable ExecuteDataTable(string databaseId, string commandText)
        {
            var dbAccess = CreateDbAcccess(databaseId);
            return dbAccess.ExecuteDataTable(commandText);
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        public static int ExecuteNonQuery(string databaseId, DbCommand command)
        {
            var dbAccess = CreateDbAcccess(databaseId);
            return dbAccess.ExecuteNonQuery(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        public static int ExecuteNonQuery(string databaseId, string commandText)
        {
            var dbAccess = CreateDbAcccess(databaseId);
            return dbAccess.ExecuteNonQuery(commandText);
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        public static object ExecuteScalar(string databaseId, DbCommand command)
        {
            DbAccess oDbAccess;

            oDbAccess = CreateDbAcccess(databaseId);
            return oDbAccess.ExecuteScalar(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        public static object ExecuteScalar(string databaseId, string commandText)
        {
            DbAccess oDbAccess;

            oDbAccess = CreateDbAcccess(databaseId);
            return oDbAccess.ExecuteScalar(commandText);
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public static DbDataReader ExecuteReader(string databaseId, DbCommand command)
        {
            var dbAccess = CreateDbAcccess(databaseId);
            return dbAccess.ExecuteReader(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public static DbDataReader ExecuteReader(string databaseId, string commandText)
        {
            var dbAccess = CreateDbAcccess(databaseId);
            return dbAccess.ExecuteReader(commandText);
        }

        /// <summary>
        /// 將 DataTable 的異動寫入資料庫。 
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="dataTable">資料表。</param>
        /// <param name="insertCommand">新增命令。</param>
        /// <param name="updateCommand">更新命令。</param>
        /// <param name="deleteCommand">刪除命令。</param>
        public static int UpdateDataTable(string databaseId, DataTable dataTable, DbCommand insertCommand, DbCommand updateCommand, DbCommand deleteCommand)
        {
            DbAccess oDbAccess;

            oDbAccess = CreateDbAcccess(databaseId);
            return oDbAccess.UpdateDataTable(dataTable, insertCommand, updateCommand, deleteCommand);
        }
    }
}
