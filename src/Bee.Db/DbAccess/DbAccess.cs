using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Bee.Base;
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
        private readonly string _databaseId = string.Empty;  // Log 使用

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseId">資料庫識別。</param>
        public DbAccess(string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentException("databaseId cannot be null or empty.", nameof(databaseId));

            // 從 DbConnectionManager 取得快取的連線資訊
            var connInfo = DbConnectionManager.GetConnectionInfo(databaseId);

            DatabaseType = connInfo.DatabaseType;
            Provider = connInfo.Provider;
            _connectionString = connInfo.ConnectionString;
            _databaseId = databaseId;
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
            _databaseId = string.Empty;
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
        /// 嘗試回滾交易（忽略回滾過程中的例外）。
        /// </summary>
        private static void TryRollbackQuiet(DbTransaction tran)
        {
            if (tran?.Connection == null) return;
            try { tran.Rollback(); } catch { /* ignore */ }
        }

        #region 同步方法

        /// <summary>
        /// 執行資料庫命令。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        public DbCommandResult Execute(DbCommandSpec command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            using (var scope = CreateScope())
            {
                switch (command.Kind)
                {
                    case DbCommandKind.NonQuery:
                        return ExecuteNonQueryCore(command, scope.Connection, null);
                    case DbCommandKind.Scalar:
                        return ExecuteScalarCore(command, scope.Connection, null);
                    case DbCommandKind.DataTable:
                        return ExecuteDataTableCore(command, scope.Connection, null);
                    default:
                        throw new NotSupportedException($"Unsupported DbCommandKind: {command.Kind}.");
                }
            }
        }

        /// <summary>
        /// 使用指定的 <see cref="DbTransaction"/> 於外部連線執行資料庫命令。
        /// 適用於需明確控制交易範圍的情境，命令將綁定至傳入的交易物件。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="transaction">必填的資料庫交易物件；命令將綁定至該交易。</param>
        public DbCommandResult Execute(DbCommandSpec command, DbTransaction transaction)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            var conn = transaction.Connection
                       ?? throw new InvalidOperationException("Transaction has no associated connection.");

            switch (command.Kind)
            {
                case DbCommandKind.NonQuery:
                    return ExecuteNonQueryCore(command, conn, transaction);
                case DbCommandKind.Scalar:
                    return ExecuteScalarCore(command, conn, transaction);
                case DbCommandKind.DataTable:
                    return ExecuteDataTableCore(command, conn, transaction);
                default:
                    throw new NotSupportedException($"Unsupported DbCommandKind: {command.Kind}.");
            }
        }

        /// <summary>
        /// 批次執行多個資料庫命令；若任何一筆失敗，回滾交易並拋例外。
        /// </summary>
        /// <param name="batch">執行批次命令的描述。</param>
        public DbBatchResult ExecuteBatch(DbBatchSpec batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            if (batch.Commands == null) throw new ArgumentNullException(nameof(batch.Commands));
            if (batch.Commands.Count == 0) throw new ArgumentException("Batch contains no commands.", nameof(batch));

            var result = new DbBatchResult();

            using (var scope = CreateScope())
            {
                DbTransaction tran = null;

                try
                {
                    if (batch.UseTransaction)
                    {
                        tran = batch.IsolationLevel.HasValue
                            ? scope.Connection.BeginTransaction(batch.IsolationLevel.Value)
                            : scope.Connection.BeginTransaction();
                    }

                    for (int i = 0; i < batch.Commands.Count; i++)
                    {
                        var spec = batch.Commands[i];

                        try
                        {
                            DbCommandResult item;
                            switch (spec.Kind)
                            {
                                case DbCommandKind.NonQuery:
                                    item = ExecuteNonQueryCore(spec, scope.Connection, tran);
                                    result.RowsAffectedSum += item.RowsAffected;
                                    break;
                                case DbCommandKind.Scalar:
                                    item = ExecuteScalarCore(spec, scope.Connection, tran);
                                    break;
                                case DbCommandKind.DataTable:
                                    item = ExecuteDataTableCore(spec, scope.Connection, tran);
                                    break;
                                default:
                                    throw new NotSupportedException($"Unsupported DbCommandKind: {spec.Kind}.");
                            }

                            result.Results.Add(item);
                        }
                        catch (Exception ex)
                        {
                            // 任何指令失敗：回滾並拋出包含索引的例外
                            TryRollbackQuiet(tran);
                            throw new InvalidOperationException(
                                $"Failed to execute batch at index {i}: {spec.Kind}.", ex);
                        }
                    }

                    // 全部成功才提交
                    try { tran?.Commit(); }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to commit transaction.", ex);
                    }
                }
                finally
                {
                    if (tran != null) tran.Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="connection">資料庫連線。</param>
        /// <param name="transaction">可選的資料庫交易物件，若為 null 則不使用交易。</param>
        private DbCommandResult ExecuteNonQueryCore(
            DbCommandSpec command, DbConnection connection, DbTransaction transaction)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                if (transaction != null) cmd.Transaction = transaction;
                var rows = cmd.ExecuteNonQuery();
                return DbCommandResult.ForRowsAffected(rows);
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="connection">資料庫連線。</param>
        /// <param name="transaction">可選的資料庫交易物件，若為 null 則不使用交易。</param>
        private DbCommandResult ExecuteScalarCore(
            DbCommandSpec command, DbConnection connection, DbTransaction transaction)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                if (transaction != null) cmd.Transaction = transaction;
                var value = cmd.ExecuteScalar();
                return DbCommandResult.ForScalar(value);
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="connection">資料庫連線。</param>
        /// <param name="transaction">可選的資料庫交易物件，若為 null 則不使用交易。</param>
        private DbCommandResult ExecuteDataTableCore(
            DbCommandSpec command, DbConnection connection, DbTransaction transaction)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                if (transaction != null) cmd.Transaction = transaction;

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

        /// <summary>
        /// 執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的清單。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="command">資料庫命令描述。</param>
        /// <returns>映射為 <see cref="List{T}"/> 的結果集合。</returns>
        public List<T> Query<T>(DbCommandSpec command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            using (var scope = CreateScope())
            using (var cmd = command.CreateCommand(DatabaseType, scope.Connection))
            using (var reader = cmd.ExecuteReader())
            {
                var list = new List<T>();
                var mapper = ILMapper<T>.CreateMapFunc(reader);
                foreach (var item in ILMapper<T>.MapToEnumerable(reader, mapper))
                {
                    list.Add(item);
                }
                return list;
            }
        }

        /// <summary>
        /// 將 DataTable 的異動寫入資料庫。 
        /// </summary>
        /// <param name="spec">承載 DataTable 更新所需的資料表與三個命令描述。</param>
        /// <returns>受影響的資料列數。</returns>
        public int UpdateDataTable(DataTableUpdateSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec.DataTable == null) throw new ArgumentNullException(nameof(spec.DataTable));
            if (spec.InsertCommand == null && spec.UpdateCommand == null && spec.DeleteCommand == null)
                throw new ArgumentException("At least one of Insert/Update/Delete command spec must be provided.", nameof(spec));

            using (var scope = CreateScope())
            {
                DbCommand insert = null, update = null, delete = null;
                DbTransaction tran = null;

                try
                {
                    if (spec.UseTransaction)
                    {
                        tran = spec.IsolationLevel.HasValue
                            ? scope.Connection.BeginTransaction(spec.IsolationLevel.Value)
                            : scope.Connection.BeginTransaction();
                    }

                    if (spec.InsertCommand != null)
                        insert = spec.InsertCommand.CreateCommand(DatabaseType, scope.Connection);
                    if (spec.UpdateCommand != null)
                        update = spec.UpdateCommand.CreateCommand(DatabaseType, scope.Connection);
                    if (spec.DeleteCommand != null)
                        delete = spec.DeleteCommand.CreateCommand(DatabaseType, scope.Connection);

                    var adapter = Provider.CreateDataAdapter()
                                  ?? throw new InvalidOperationException("DbProviderFactory.CreateDataAdapter() returned null.");

                    using (adapter)
                    {
                        adapter.InsertCommand = insert;
                        adapter.UpdateCommand = update;
                        adapter.DeleteCommand = delete;

                        if (tran != null)
                        {
                            if (insert != null) insert.Transaction = tran;
                            if (update != null) update.Transaction = tran;
                            if (delete != null) delete.Transaction = tran;
                        }

                        int affected = adapter.Update(spec.DataTable);

                        tran?.Commit();
                        return affected;
                    }
                }
                catch
                {
                    tran?.Rollback();
                    throw;
                }
                finally
                {
                    if (insert != null) insert.Dispose();
                    if (update != null) update.Dispose();
                    if (delete != null) delete.Dispose();
                    tran?.Dispose();
                }
            }
        }

        #endregion

        #region 同步版本的簡易方法

        /// <summary>
        /// 執行 SQL 指令，傳回異動筆數。
        /// </summary>
        /// <param name="commandText">要執行的 SQL 陳述式，只能使用 {0}, {1} 格式。</param>
        /// <param name="values">位置參數值，依序對應 {0}, {1} ...</param>
        /// <returns>受影響的資料列數。</returns>
        public int ExecuteNonQuery(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, commandText, values);
            return Execute(spec).RowsAffected;
        }

        /// <summary>
        /// 執行 SQL 指令，傳回單一值。
        /// </summary>
        /// <param name="commandText">要執行的 SQL 陳述式，只能使用 {0}, {1} 格式。</param>
        /// <param name="values">位置參數值，依序對應 {0}, {1} ...</param>
        /// <returns>查詢結果的第一個欄位值。</returns>
        public object ExecuteScalar(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar, commandText, values);
            return Execute(spec).Scalar;
        }

        /// <summary>
        /// 執行 SQL 指令，傳回資料表。
        /// </summary>
        /// <param name="commandText">要執行的 SQL 陳述式，只能使用 {0}, {1} 格式。</param>
        /// <param name="values">位置參數值，依序對應 {0}, {1} ...</param>
        /// <returns>查詢結果的 <see cref="DataTable"/>。</returns>
        public DataTable ExecuteDataTable(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.DataTable, commandText, values);
            return Execute(spec).Table;
        }

        #endregion

        #region 非同步方法

        /// <summary>
        /// 非同步執行資料庫命令。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public async Task<DbCommandResult> ExecuteAsync(
            DbCommandSpec command, CancellationToken cancellationToken = default)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            using (var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false))
            {
                switch (command.Kind)
                {
                    case DbCommandKind.NonQuery:
                        return await ExecuteNonQueryCoreAsync(command, scope.Connection, null, cancellationToken).ConfigureAwait(false);
                    case DbCommandKind.Scalar:
                        return await ExecuteScalarCoreAsync(command, scope.Connection, null, cancellationToken).ConfigureAwait(false);
                    case DbCommandKind.DataTable:
                        return await ExecuteDataTableCoreAsync(command, scope.Connection, null, cancellationToken).ConfigureAwait(false);
                    default:
                        throw new NotSupportedException($"Unsupported DbCommandKind: {command.Kind}.");
                }
            }
        }

        /// <summary>
        /// 使用指定的 <see cref="DbTransaction"/> 於外部連線非同步執行資料庫命令。
        /// 適用於需明確控制交易範圍的情境，命令將綁定至傳入的交易物件。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="transaction">必填的資料庫交易物件；命令將綁定至該交易。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public Task<DbCommandResult> ExecuteAsync(
            DbCommandSpec command, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            var conn = transaction.Connection
                       ?? throw new InvalidOperationException("Transaction has no associated connection.");

            switch (command.Kind)
            {
                case DbCommandKind.NonQuery:
                    return ExecuteNonQueryCoreAsync(command, conn, transaction, cancellationToken);
                case DbCommandKind.Scalar:
                    return ExecuteScalarCoreAsync(command, conn, transaction, cancellationToken);
                case DbCommandKind.DataTable:
                    return ExecuteDataTableCoreAsync(command, conn, transaction, cancellationToken);
                default:
                    throw new NotSupportedException($"Unsupported DbCommandKind: {command.Kind}.");
            }
        }

        /// <summary>
        /// 非同步批次執行多個資料庫命令；若任何一筆失敗，回滾交易並拋例外。
        /// </summary>
        /// <param name="batch">執行批次命令的描述。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        public async Task<DbBatchResult> ExecuteBatchAsync(DbBatchSpec batch, CancellationToken cancellationToken = default)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            if (batch.Commands == null) throw new ArgumentNullException(nameof(batch.Commands));
            if (batch.Commands.Count == 0) throw new ArgumentException("Batch contains no commands.", nameof(batch));

            var result = new DbBatchResult();

            using (var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false))
            {
                DbTransaction tran = null;

                try
                {
                    if (batch.UseTransaction)
                    {
#if NET8_0_OR_GREATER
                        // .NET 8.0+ 有 BeginTransactionAsync
                        tran = batch.IsolationLevel.HasValue
                            ? await scope.Connection.BeginTransactionAsync(batch.IsolationLevel.Value, cancellationToken).ConfigureAwait(false)
                            : await scope.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
#else
                        // .NET Standard 2.0 無 BeginTransactionAsync，此處以同步 BeginTransaction 啟動交易
                        tran = batch.IsolationLevel.HasValue
                            ? scope.Connection.BeginTransaction(batch.IsolationLevel.Value)
                            : scope.Connection.BeginTransaction();
#endif
                    }

                    for (int i = 0; i < batch.Commands.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var spec = batch.Commands[i];

                        try
                        {
                            DbCommandResult item;
                            switch (spec.Kind)
                            {
                                case DbCommandKind.NonQuery:
                                    item = await ExecuteNonQueryCoreAsync(spec, scope.Connection, tran, cancellationToken).ConfigureAwait(false);
                                    result.RowsAffectedSum += item.RowsAffected;
                                    break;
                                case DbCommandKind.Scalar:
                                    item = await ExecuteScalarCoreAsync(spec, scope.Connection, tran, cancellationToken).ConfigureAwait(false);
                                    break;
                                case DbCommandKind.DataTable:
                                    item = await ExecuteDataTableCoreAsync(spec, scope.Connection, tran, cancellationToken).ConfigureAwait(false);
                                    break;
                                default:
                                    throw new NotSupportedException($"Unsupported DbCommandKind: {spec.Kind}.");
                            }

                            result.Results.Add(item);
                        }
                        catch (Exception ex)
                        {
                            // 任何指令失敗：回滾並拋出包含索引的例外
                            TryRollbackQuiet(tran);
                            throw new InvalidOperationException(
                                $"Failed to execute batch at index {i}: {spec.Kind}.", ex);
                        }
                    }

                    // 全部成功才提交
                    try { tran?.Commit(); }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to commit transaction.", ex);
                    }
                }
                finally
                {
                    if (tran != null) tran.Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="connection">資料庫連線。</param>
        /// <param name="transaction">可選的資料庫交易物件，若為 null 則不使用交易。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        private async Task<DbCommandResult> ExecuteNonQueryCoreAsync(
            DbCommandSpec command, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                if (transaction != null) cmd.Transaction = transaction;
                var rows = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return DbCommandResult.ForRowsAffected(rows);
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="connection">資料庫連線。</param>
        /// <param name="transaction">可選的資料庫交易物件，若為 null 則不使用交易。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        private async Task<DbCommandResult> ExecuteScalarCoreAsync(
            DbCommandSpec command, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                if (transaction != null) cmd.Transaction = transaction;
                var value = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return DbCommandResult.ForScalar(value);
            }
        }

        /// <summary>
        /// 非同步執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="connection">資料庫連線。</param>
        /// <param name="transaction">可選的資料庫交易物件，若為 null 則不使用交易。</param>
        /// <param name="cancellationToken">取消權杖，可於長時間執行的命令中用於取消等待。</param>
        private async Task<DbCommandResult> ExecuteDataTableCoreAsync(
            DbCommandSpec command, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                if (transaction != null) cmd.Transaction = transaction; // ← 先設定交易

                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    var table = new DataTable("DataTable");
                    table.Load(reader);
                    DataSetFunc.UpperColumnName(table);
                    return DbCommandResult.ForTable(table);
                }
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

        #endregion

        #region 非同步版本的簡易方法

        /// <summary>
        /// 非同步執行 SQL 指令，傳回異動筆數。
        /// </summary>
        /// <param name="commandText">要執行的 SQL 陳述式，只能使用 {0}, {1} 格式。</param>
        /// <param name="values">位置參數值，依序對應 {0}, {1} ...</param>
        /// <returns>受影響的資料列數。</returns>
        public async Task<int> ExecuteNonQueryAsync(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, commandText, values);
            return (await ExecuteAsync(spec)).RowsAffected;
        }

        /// <summary>
        /// 非同步執行 SQL 指令，傳回單一值。
        /// </summary>
        /// <param name="commandText">要執行的 SQL 陳述式，只能使用 {0}, {1} 格式。</param>
        /// <param name="values">位置參數值，依序對應 {0}, {1} ...</param>
        /// <returns>查詢結果的第一個欄位值。</returns>
        public async Task<object> ExecuteScalarAsync(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar, commandText, values);
            return (await ExecuteAsync(spec)).Scalar;
        }

        /// <summary>
        /// 非同步執行 SQL 指令，傳回資料表。
        /// </summary>
        /// <param name="commandText">要執行的 SQL 陳述式，只能使用 {0}, {1} 格式。</param>
        /// <param name="values">位置參數值，依序對應 {0}, {1} ...</param>
        /// <returns>查詢結果的 <see cref="DataTable"/>。</returns>
        public async Task<DataTable> ExecuteDataTableAsync(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.DataTable, commandText, values);
            return (await ExecuteAsync(spec)).Table;
        }

        #endregion

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"DbAccess {{ DatabaseType = {DatabaseType}, Provider = {Provider?.GetType().Name} }}";
        }
    }
}
