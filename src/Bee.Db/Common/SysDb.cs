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
    /// 系統資料庫操作類別。
    /// </summary>
    public static class SysDb
    {
        /// <summary>
        /// 建立資料庫存取物件。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        private static DbAccess CreateDbAccess(string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentException("databaseId cannot be null or empty.", nameof(databaseId));

            var database = CacheFunc.GetDatabaseItem(databaseId);
            if (database == null)
                throw new InvalidOperationException($"Failed to create DbAccess: DatabaseItem for id '{databaseId}' was not found.");

            return new DbAccess(database);
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
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        public static object ExecuteScalar(string databaseId, DbCommand command)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteScalar(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        public static object ExecuteScalar(string databaseId, string commandText)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteScalar(commandText);
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public static Task<object> ExecuteScalarAsync(string databaseId, DbCommand command, CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteScalarAsync(command, cancellationToken);
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public static Task<object> ExecuteScalarAsync(string databaseId, string commandText, CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteScalarAsync(commandText, cancellationToken);
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// 呼叫端需在使用完畢後呼叫 reader.Dispose()
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public static DbDataReader ExecuteReader(string databaseId, DbCommand command)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteReader(command);
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// 呼叫端需在使用完畢後呼叫 reader.Dispose()
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public static Task<DbDataReader> ExecuteReaderAsync(string databaseId, DbCommand command, CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.ExecuteReaderAsync(command, cancellationToken);
        }

        /// <summary>
        /// 執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的可列舉集合。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        /// <returns>返回 <see cref="IEnumerable{T}"/>，允許逐筆讀取查詢結果。</returns>
        public static IEnumerable<T> Query<T>(string databaseId, DbCommand command)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.Query<T>(command);
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
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        /// <returns>映射為 <see cref="List{T}"/> 的結果集合。</returns>
        public static Task<List<T>> QueryAsync<T>(string databaseId, DbCommand command, CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.QueryAsync<T>(command, cancellationToken);
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

#if NET8_0_OR_GREATER
        /// <summary>
        /// 非同步串流查詢結果，每次讀取一列並映射為 <typeparamref name="T"/>。
        /// 注意：呼叫端需逐項列舉以完整釋放連線資源。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖。</param>
        public static async IAsyncEnumerable<T> QueryStreamAsync<T>(
            string databaseId,
            DbCommand command,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            await foreach (var item in dbAccess.QueryStreamAsync<T>(command, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        /// <summary>
        /// 非同步串流查詢結果，每次讀取一列並映射為 <typeparamref name="T"/>。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <param name="cancellationToken">取消權杖。</param>
        public static async IAsyncEnumerable<T> QueryStreamAsync<T>(
            string databaseId,
            string commandText,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var dbAccess = CreateDbAccess(databaseId);
            await foreach (var item in dbAccess.QueryStreamAsync<T>(commandText, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
#endif

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
            var dbAccess = CreateDbAccess(databaseId);
            return dbAccess.UpdateDataTable(dataTable, insertCommand, updateCommand, deleteCommand);
        }
    }
}
