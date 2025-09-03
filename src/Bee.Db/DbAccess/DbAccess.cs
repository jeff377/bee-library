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
        private readonly DbConnection _externalConnection = null;
        private readonly string _connectionString = string.Empty;

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

            DatabaseType = database.DatabaseType;
            Provider = DbProviderManager.GetFactory(database.DatabaseType)
                       ?? throw new InvalidOperationException($"Unknown database type: {database.DatabaseType}.");
            _connectionString = database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("DatabaseItem.GetConnectionString() returned null or empty.");
        }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="externalConnection">由外部提供的 DbConnection 建立 DbAccess，連線生命週期由外部管理。</param>
        public DbAccess(DbConnection externalConnection)
        {
            _externalConnection = externalConnection ?? throw new ArgumentNullException(nameof(externalConnection));
            DatabaseType = BackendInfo.DatabaseType;
            Provider = DbProviderManager.GetFactory(DatabaseType)
                ?? throw new InvalidOperationException($"Unknown database type: {DatabaseType}.");
        }

        #endregion

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        public DatabaseType DatabaseType { get; }

        /// <summary>
        /// 資料庫來源提供者。
        /// </summary>
        public DbProviderFactory Provider { get; }

        /// <summary>
        /// 建立連線範圍，會自動決定使用外部連線或自行建立連線。
        /// </summary>
        private DbConnectionScope CreateScope()
        {
            // 這個型別假設已有，且能依「外部連線或 provider+cs」建立對應的 scope
            return DbConnectionScope.Create(_externalConnection, Provider, _connectionString);
        }

        /// <summary>
        /// 非同步建立連線範圍，會自動決定使用外部連線或自行建立連線。
        /// </summary>
        private Task<DbConnectionScope> CreateScopeAsync(CancellationToken cancellationToken = default)
        {
            return DbConnectionScope.CreateAsync(_externalConnection, Provider, _connectionString, cancellationToken);
        }

        /// <summary>
        /// 執行資料庫命令。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        public DbCommandResult Execute(DbCommandSpec command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            switch (command.Kind)
            {
                case DbCommandKind.NonQuery:
                    return ExecuteNonQuery(command);
                case DbCommandKind.Scalar:
                    return ExecuteScalar(command);
                case DbCommandKind.DataTable:
                    return ExecuteDataTable(command);
                default:
                    throw new NotSupportedException($"Unsupported DbCommandKind: {command.Kind}.");
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        private DbCommandResult ExecuteDataTable(DbCommandSpec command)
        {
            using (var scope = CreateScope())
            {
                using (var cmd = command.CreateCommand(DatabaseType, scope.Connection))
                {

                    var adapter = Provider.CreateDataAdapter()
                        ?? throw new InvalidOperationException("DbProviderFactory.CreateDataAdapter() returned null.");

                    using (adapter)
                    {
                        adapter.SelectCommand = cmd;
                        var table = new DataTable("DataTable");
                        adapter.Fill(table);
                        DataSetFunc.UpperColumnName(table);
                        return DbCommandResult.ForTable(table);
                    }
                }
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        private DbCommandResult ExecuteNonQuery(DbCommandSpec command)
        {
            using (var scope = CreateScope())
            {
                using (var cmd = command.CreateCommand(DatabaseType, scope.Connection))
                {
                    int rows = cmd.ExecuteNonQuery();
                    return DbCommandResult.ForRowsAffected(rows);
                }
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        private DbCommandResult ExecuteScalar(DbCommandSpec command)
        {
            using (var scope = CreateScope())
            {
                using (var cmd = command.CreateCommand(DatabaseType, scope.Connection))
                {
                    var value = cmd.ExecuteScalar();
                    return DbCommandResult.ForScalar(value);
                }
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// 呼叫端需在使用完畢後呼叫 reader.Dispose()
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        private DbDataReader ExecuteReader(DbCommandSpec command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var scope = CreateScope();
            try
            {
                var cmd = command.CreateCommand(DatabaseType, scope.Connection);
                // CloseConnection: reader 關閉時一併關閉 connection
                return cmd.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch
            {
                // ExecuteReader 失敗要自行清理連線
                scope.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的可列舉集合。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="command">資料庫命令描述。</param>
        /// <returns>返回 <see cref="IEnumerable{T}"/>，允許逐筆讀取查詢結果。</returns>
        public IEnumerable<T> Query<T>(DbCommandSpec command)
        {
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
        /// 非同步執行資料庫命令。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public async Task<DbCommandResult> ExecuteAsync(DbCommandSpec command, CancellationToken cancellationToken = default)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            switch (command.Kind)
            {
                case DbCommandKind.NonQuery:
                    return await ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
                case DbCommandKind.Scalar:
                    return await ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
                case DbCommandKind.DataTable:
                    return await ExecuteDataTableAsync(command, cancellationToken).ConfigureAwait(false);
                default:
                    throw new NotSupportedException($"Unsupported DbCommandKind: {command.Kind}.");
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        private async Task<DbCommandResult> ExecuteDataTableAsync(DbCommandSpec command, CancellationToken cancellationToken = default)
        {
            using (var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false))
            using (var cmd = command.CreateCommand(DatabaseType, scope.Connection))
            using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                var table = new DataTable("DataTable");
                table.Load(reader);
                DataSetFunc.UpperColumnName(table);
                return DbCommandResult.ForTable(table);
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        private async Task<DbCommandResult> ExecuteNonQueryAsync(DbCommandSpec command, CancellationToken cancellationToken = default)
        {
            using (var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false))
            using (var cmd = command.CreateCommand(DatabaseType, scope.Connection))
            {
                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return DbCommandResult.ForRowsAffected(rows);
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        private async Task<DbCommandResult> ExecuteScalarAsync(DbCommandSpec command, CancellationToken cancellationToken = default)
        {
            using (var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false))
            using (var cmd = command.CreateCommand(DatabaseType, scope.Connection))
            {
                var value = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return DbCommandResult.ForScalar(value);
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// 呼叫端需在使用完畢後呼叫 reader.Dispose()
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        private async Task<DbDataReader> ExecuteReaderAsync(DbCommandSpec command, CancellationToken cancellationToken = default)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            // 用 Scope 統一管理連線建立/開啟；成功後不在這裡 Dispose Scope，
            // 讓連線生命週期交由 reader（自建連線時）或外部（外部連線時）決定。
            var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var cmd = command.CreateCommand(DatabaseType, scope.Connection);

                // 自行建立連線才使用 CloseConnection（reader.Dispose() 會關掉連線）；
                // 若使用外部連線，避免關閉外部連線。
                var behavior = (_externalConnection == null)
                    ? CommandBehavior.CloseConnection
                    : CommandBehavior.Default;

                return await cmd.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // 失敗時才釋放 scope（避免連線洩漏）
                scope.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的清單。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        /// <returns>映射為 <see cref="List{T}"/> 的結果集合。</returns>
        public async Task<List<T>> QueryAsync<T>(DbCommandSpec command, CancellationToken cancellationToken = default)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            using (var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false))
            using (var cmd = command.CreateCommand(DatabaseType, scope.Connection))
            using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                var list = new List<T>();
                var mapper = ILMapper<T>.CreateMapFunc(reader); // 以目前欄位集建立映射

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    list.Add(mapper(reader));
                }
                return list;
            }
        }

        /// <summary>
        /// 將 DataTable 的異動寫入資料庫。 
        /// </summary>
        /// <param name="updateSpec">承載 DataTable 更新所需的資料表與三個命令描。</param>
        /// <returns>受影響的資料列數。</returns>
        public int UpdateDataTable(DataTableUpdateSpec updateSpec)
        {
            if (updateSpec == null) throw new ArgumentNullException(nameof(updateSpec));
            if (updateSpec.DataTable == null) throw new ArgumentNullException(nameof(updateSpec.DataTable));
            if (updateSpec.InsertCommand == null && updateSpec.UpdateCommand == null && updateSpec.DeleteCommand == null)
                throw new ArgumentException("At least one of Insert/Update/Delete command spec must be provided.", nameof(updateSpec));

            using (var scope = CreateScope())
            {
                DbCommand insert = null, update = null, delete = null;

                try
                {
                    if (updateSpec.InsertCommand != null)
                    {
                        insert = updateSpec.InsertCommand.CreateCommand(DatabaseType, scope.Connection);
                    }
                    if (updateSpec.UpdateCommand != null)
                    {
                        update = updateSpec.UpdateCommand.CreateCommand(DatabaseType, scope.Connection);
                    }
                    if (updateSpec.DeleteCommand != null)
                    {
                        delete = updateSpec.DeleteCommand.CreateCommand(DatabaseType, scope.Connection);
                    }

                    var adapter = Provider.CreateDataAdapter()
                                  ?? throw new InvalidOperationException("DbProviderFactory.CreateDataAdapter() returned null.");

                    using (adapter)
                    {
                        adapter.InsertCommand = insert;
                        adapter.UpdateCommand = update;
                        adapter.DeleteCommand = delete;

                        return adapter.Update(updateSpec.DataTable);
                    }
                }
                finally
                {
                    if (insert != null) insert.Dispose();
                    if (update != null) update.Dispose();
                    if (delete != null) delete.Dispose();
                }
            }
        }
    }
}
