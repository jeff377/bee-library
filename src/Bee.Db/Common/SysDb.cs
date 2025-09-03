using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Bee.Cache;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫命令執行器。
    /// </summary>
    public static class SysDb
    {
        /// <summary>
        /// 建立資料庫存取物件。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        private static DbAccess CreateDbAccess(string databaseId)
        {
            return new DbAccess(databaseId);
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        public static DataTable ExecuteDataTable(string databaseId, DbCommand command)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteDataTable(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        public static DataTable ExecuteDataTable(string databaseId, string commandText)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteDataTable(commandText);
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public static Task<DataTable> ExecuteDataTableAsync(string databaseId, DbCommand command, CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteDataTableAsync(command, cancellationToken);
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public static Task<DataTable> ExecuteDataTableAsync(string databaseId, string commandText, CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteDataTableAsync(commandText, cancellationToken);
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        public static int ExecuteNonQuery(string databaseId, DbCommand command)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteNonQuery(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        public static int ExecuteNonQuery(string databaseId, string commandText)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteNonQuery(commandText);
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回異動筆數。
        /// </summary>
        public static Task<int> ExecuteNonQueryAsync(string databaseId, DbCommand command, CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteNonQueryAsync(command, cancellationToken);
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public static Task<int> ExecuteNonQueryAsync(string databaseId, string commandText, CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteNonQueryAsync(commandText, cancellationToken);
        }
        /// <summary>
        /// 執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的可列舉集合。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <returns>返回 <see cref="IEnumerable{T}"/>，允許逐筆讀取查詢結果。</returns>
        public static IEnumerable<T> Query<T>(string databaseId, string commandText)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.Query<T>(commandText);
        }

        /// <summary>
        /// 非同步執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的清單。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        /// <returns>映射為 <see cref="List{T}"/> 的結果集合。</returns>
        public static Task<List<T>> QueryAsync<T>(string databaseId, string commandText, CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.QueryAsync<T>(commandText, cancellationToken);
        }
    }
}
