using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Bee.Base;
using Bee.Cache;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫存取物件。
    /// </summary>
    public class DbAccess
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        public DbAccess(string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentException("databaseId cannot be null or empty.", nameof(databaseId));

            var database = CacheFunc.GetDatabaseItem(databaseId);
            if (database == null)
                throw new InvalidOperationException($"Failed to create DbAccess: DatabaseItem for id '{databaseId}' was not found.");

            Provider = DbProviderManager.GetFactory(database.DatabaseType)
                       ?? throw new InvalidOperationException($"Unknown database type: {database.DatabaseType}.");
            ConnectionString = database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException("DatabaseItem.GetConnectionString() returned null or empty.");
        }

        #endregion

        /// <summary>
        /// 資料庫來源提供者。
        /// </summary>
        public DbProviderFactory Provider { get; private set; }

        /// <summary>
        /// 資料庫連線字串。
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// 使用 CreateCommand 的預設命令逾時（秒）。
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// 建立資料庫連線。
        /// </summary>
        public DbConnection CreateConnection()
        {
            var connection = Provider.CreateConnection();
            if (connection == null)
                throw new InvalidOperationException("Failed to create a database connection: DbProviderFactory.CreateConnection() returned null.");

            connection.ConnectionString = ConnectionString;
            return connection;
        }

        /// <summary>
        /// 開啟資料庫連線。
        /// </summary>
        private DbConnection OpenConnection()
        {
            var connection = CreateConnection();
            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                // 失敗時也要確保釋放
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 非同步開啟資料庫連線。
        /// </summary>
        private async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = CreateConnection();
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 依全域上限 <see cref="BackendInfo.MaxDbCommandTimeout"/> 套用命令逾時限制。
        /// </summary>
        /// <param name="timeoutSeconds">要求的逾時秒數。</param>
        /// <returns>經上限處理後的逾時秒數。</returns>
        private static int CapTimeoutSeconds(int timeoutSeconds)
        {
            int cap = BackendInfo.MaxDbCommandTimeout;
            // 不控管 → 原值
            if (cap <= 0) { return timeoutSeconds; }
            // 無限或負數 → 套上限
            if (timeoutSeconds <= 0) { return cap; }
            // 驗證是否套上限
            return timeoutSeconds > cap ? cap : timeoutSeconds;
        }

        /// <summary>
        /// 對指定 <see cref="DbCommand"/> 的 <see cref="DbCommand.CommandTimeout"/> 套用全域上限。
        /// </summary>
        /// <param name="command">要處理的資料庫命令。</param>
        private static void CapCommandTimeout(DbCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            command.CommandTimeout = CapTimeoutSeconds(command.CommandTimeout);
        }

        /// <summary>
        /// 建立資料庫命令。
        /// </summary>
        /// <param name="commandType">SQL 命令類型。</param>
        /// <param name="commandText">SQL 陳述式或預存程序。</param>
        private DbCommand CreateCommand(CommandType commandType, string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentException("commandText cannot be null or empty.", nameof(commandText));

            var command = Provider.CreateCommand();
            if (command == null)
                throw new InvalidOperationException("DbProviderFactory.CreateCommand() returned null.");

            command.CommandType = commandType;
            command.CommandText = commandText;
            command.CommandTimeout = CapTimeoutSeconds(CommandTimeout);
            return command;
        }

        /// <summary>
        /// 建立資料庫命令。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        private DbCommand CreateCommand(string commandText)
        {
            return CreateCommand(CommandType.Text, commandText);
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        public DataTable ExecuteDataTable(DbCommand command)
        {
            CapCommandTimeout(command);   // 套用命令逾時限制

            using (var connection = OpenConnection())
            {
                command.Connection = connection;

                var adapter = Provider.CreateDataAdapter()
                    ?? throw new InvalidOperationException("DbProviderFactory.CreateDataAdapter() returned null.");

                using (adapter)
                {
                    adapter.SelectCommand = command;
                    var table = new DataTable("DataTable");
                    adapter.Fill(table);
                    DataSetFunc.UpperColumnName(table);
                    return table;
                }
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        public DataTable ExecuteDataTable(string commandText)
        {
            using (var command = CreateCommand(commandText))
            {
                return ExecuteDataTable(command); // command 將於此 using 結束時 Dispose
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public async Task<DataTable> ExecuteDataTableAsync(DbCommand command, CancellationToken cancellationToken = default)
        {
            CapCommandTimeout(command);
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                command.Connection = connection;
                using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    var table = new DataTable("DataTable");
                    table.Load(reader);
                    DataSetFunc.UpperColumnName(table);
                    return table;
                }
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public async Task<DataTable> ExecuteDataTableAsync(string commandText, CancellationToken cancellationToken = default)
        {
            using (var command = CreateCommand(commandText))
            {
                return await ExecuteDataTableAsync(command, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        public int ExecuteNonQuery(DbCommand command)
        {
            CapCommandTimeout(command);   // 套用命令逾時限制
            using (var connection = OpenConnection())
            {
                command.Connection = connection;
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        public int ExecuteNonQuery(string commandText)
        {
            using (var command = CreateCommand(commandText))
            {
                return ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public async Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken = default)
        {
            CapCommandTimeout(command);  // 套用命令逾時限制
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                command.Connection = connection;
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public async Task<int> ExecuteNonQueryAsync(string commandText, CancellationToken cancellationToken = default)
        {
            using (var command = CreateCommand(commandText))
            {
                return await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        public object ExecuteScalar(DbCommand command)
        {
            CapCommandTimeout(command);   // 套用命令逾時限制
            using (var connection = OpenConnection())
            {
                command.Connection = connection;
                return command.ExecuteScalar();
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        public object ExecuteScalar(string commandText)
        {
            using (var command = CreateCommand(commandText))
            {
                return ExecuteScalar(command);
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public async Task<object> ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken = default)
        {
            CapCommandTimeout(command);
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                command.Connection = connection;
                return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public async Task<object> ExecuteScalarAsync(string commandText, CancellationToken cancellationToken = default)
        {
            using (var command = CreateCommand(commandText))
            {
                return await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// 呼叫端需在使用完畢後呼叫 reader.Dispose()
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public DbDataReader ExecuteReader(DbCommand command)
        {
            CapCommandTimeout(command);   // 套用命令逾時限制

            var connection = OpenConnection();
            try
            {
                command.Connection = connection;
                // CloseConnection: reader 關閉時一併關閉 connection
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch
            {
                // ExecuteReader 失敗要自行清理連線
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// 呼叫端需在使用完畢後呼叫 reader.Dispose()
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public async Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CancellationToken cancellationToken = default)
        {
            CapCommandTimeout(command);
            var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                command.Connection = connection;
                return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的可列舉集合。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="command">資料庫命令。</param>
        /// <returns>返回 <see cref="IEnumerable{T}"/>，允許逐筆讀取查詢結果。</returns>
        public IEnumerable<T> Query<T>(DbCommand command)
        {
            CapCommandTimeout(command);   // 套用命令逾時限制

            // 使用 command 執行資料庫查詢，並取得 DbDataReader
            var reader = ExecuteReader(command);
            var mapper = ILMapper<T>.CreateMapFunc(reader);
            // 延遲執行，不能使用 using，會造成連線被提早關閉
            try
            {
                foreach (var item in ILMapper<T>.MapToEnumerable(reader, mapper))
                {
                    yield return item;
                }
            }
            finally
            {
                reader.Dispose(); // 迭代結束後才關閉 reader
            }
        }

        /// <summary>
        /// 執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的可列舉集合。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <returns>返回 <see cref="IEnumerable{T}"/>，允許逐筆讀取查詢結果。</returns>
        public IEnumerable<T> Query<T>(string commandText)
        {
            using (var command = CreateCommand(commandText))
            {
                var reader = ExecuteReader(command); // reader.Dispose 時會關 connection
                var mapper = ILMapper<T>.CreateMapFunc(reader);
                try
                {
                    foreach (var item in ILMapper<T>.MapToEnumerable(reader, mapper))
                        yield return item;
                }
                finally
                {
                    reader.Dispose(); // 於列舉完成時關閉
                }
            } // command 於列舉完成時 Dispose
        }

        /// <summary>
        /// 非同步執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的清單。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        /// <returns>映射為 <see cref="List{T}"/> 的結果集合。</returns>
        public async Task<List<T>> QueryAsync<T>(DbCommand command, CancellationToken cancellationToken = default)
        {
            CapCommandTimeout(command);

            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                command.Connection = connection;

                using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    var list = new List<T>();
                    var mapper = ILMapper<T>.CreateMapFunc(reader); // 以目前列映射

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        // 假設 mapper 以目前 reader 指向的列做映射
                        list.Add(mapper(reader));
                    }
                    return list;
                }
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的清單。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        /// <returns>映射為 <see cref="List{T}"/> 的結果集合。</returns>
        public async Task<List<T>> QueryAsync<T>(string commandText, CancellationToken cancellationToken = default)
        {
            using (var command = CreateCommand(commandText))
            {
                return await QueryAsync<T>(command, cancellationToken).ConfigureAwait(false);
            }
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// 非同步串流查詢結果，每次讀取一列並映射為 <typeparamref name="T"/>。
        /// 注意：呼叫端需逐項列舉以完整釋放連線資源。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="command">資料庫命令。</param>
        /// <param name="cancellationToken">取消權杖。</param>
        public async IAsyncEnumerable<T> QueryStreamAsync<T>(
            DbCommand command,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            CapCommandTimeout(command);

            var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            DbDataReader reader = null;
            try
            {
                command.Connection = connection;
                // 交由 reader 關閉連線
                reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken)
                                      .ConfigureAwait(false);

                var mapper = ILMapper<T>.CreateMapFunc(reader);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return mapper(reader);   // ✅ 允許：try-finally，且方法內沒有任何 catch
                }
            }
            finally
            {
                // 若 reader 已建立 → Dispose() 會連帶關閉 connection（CloseConnection）
                // 若尚未建立就拋例外 → 這裡補關 connection，避免外洩
                if (reader != null) reader.Dispose();
                else connection.Dispose();
            }
        }

        /// <summary>
        /// 非同步串流查詢結果，每次讀取一列並映射為 <typeparamref name="T"/>。
        /// 注意：呼叫端需逐項列舉以完整釋放連線資源。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <param name="cancellationToken">取消權杖。</param>
        public async IAsyncEnumerable<T> QueryStreamAsync<T>(
            string commandText,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var command = CreateCommand(commandText);
            await foreach (var item in QueryStreamAsync<T>(command, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
#endif

        /// <summary>
        /// 將 DataTable 的異動寫入資料庫。 
        /// </summary>
        /// <param name="dataTable">資料表。</param>
        /// <param name="insertCommand">新增命令。</param>
        /// <param name="updateCommand">更新命令。</param>
        /// <param name="deleteCommand">刪除命令。</param>
        /// <param name="disposeCommands">
        /// 指定是否於方法執行完成後自動釋放 <paramref name="insertCommand"/>、
        /// <paramref name="updateCommand"/> 與 <paramref name="deleteCommand"/>。
        /// 若設為 <c>false</c>，表示命令物件的生命週期由呼叫端管理；
        /// 若設為 <c>true</c>，則方法會於結束時呼叫 <see cref="IDisposable.Dispose"/> 釋放命令資源。
        /// </param>
        /// <returns>受影響的資料列數。</returns>
        public int UpdateDataTable(DataTable dataTable, DbCommand insertCommand, DbCommand updateCommand, DbCommand deleteCommand, bool disposeCommands)
        {
            if (dataTable == null) throw new ArgumentNullException(nameof(dataTable));

            try
            {
                using (var connection = OpenConnection())
                {
                    if (insertCommand != null) { CapCommandTimeout(insertCommand); insertCommand.Connection = connection; }
                    if (updateCommand != null) { CapCommandTimeout(updateCommand); updateCommand.Connection = connection; }
                    if (deleteCommand != null) { CapCommandTimeout(deleteCommand); deleteCommand.Connection = connection; }

                    var adapter = Provider.CreateDataAdapter();
                    if (adapter == null)
                        throw new InvalidOperationException("DbProviderFactory.CreateDataAdapter() returned null.");

                    using (adapter)
                    {
                        adapter.InsertCommand = insertCommand;
                        adapter.UpdateCommand = updateCommand;
                        adapter.DeleteCommand = deleteCommand;
                        return adapter.Update(dataTable);
                    }
                }
            }
            finally 
            {
                if (disposeCommands)
                {
                    if (insertCommand != null) insertCommand.Dispose();
                    if (updateCommand != null) updateCommand.Dispose();
                    if (deleteCommand != null) deleteCommand.Dispose();
                }

            }


        }

    }
}
