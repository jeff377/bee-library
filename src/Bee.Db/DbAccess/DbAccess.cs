using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Bee.Base;
using Bee.Base.Data;
using Bee.Define;

using Bee.Db;
using Bee.Db.Manager;

namespace Bee.Db.DbAccess
{
    /// <summary>
    /// Provides database access operations including query execution, batch commands, and DataTable updates.
    /// </summary>
    public class DbAccess
    {
        private readonly DbConnection _externalConnection = null;
        private readonly string _connectionString = string.Empty;
        private readonly string _databaseId = string.Empty;  // Used for logging

        #region 建構函式

        /// <summary>
        /// Initializes a new instance of <see cref="DbAccess"/> for the specified database identifier.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public DbAccess(string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentException("databaseId cannot be null or empty.", nameof(databaseId));

            // Retrieve cached connection information from DbConnectionManager
            var connInfo = DbConnectionManager.GetConnectionInfo(databaseId);

            DatabaseType = connInfo.DatabaseType;
            Provider = connInfo.Provider;
            _connectionString = connInfo.ConnectionString;
            _databaseId = databaseId;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DbAccess"/> using an externally managed <see cref="DbConnection"/>.
        /// The connection lifetime is managed by the caller.
        /// </summary>
        /// <param name="externalConnection">The externally provided database connection.</param>
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
        /// Gets the database type.
        /// </summary>
        public DatabaseType DatabaseType { get; }

        /// <summary>
        /// Gets the database provider factory.
        /// </summary>
        public DbProviderFactory Provider { get; }

        /// <summary>
        /// Creates a connection scope, automatically choosing between the external connection and a newly created one.
        /// </summary>
        private DbConnectionScope CreateScope()
        {
            return DbConnectionScope.Create(_externalConnection, Provider, _connectionString);
        }

        /// <summary>
        /// Asynchronously creates a connection scope, automatically choosing between the external connection and a newly created one.
        /// </summary>
        private Task<DbConnectionScope> CreateScopeAsync(CancellationToken cancellationToken = default)
        {
            return DbConnectionScope.CreateAsync(_externalConnection, Provider, _connectionString, cancellationToken);
        }

        /// <summary>
        /// Attempts to roll back a transaction, silently ignoring any exceptions during rollback.
        /// </summary>
        private static void TryRollbackQuiet(DbTransaction tran)
        {
            if (tran?.Connection == null) return;
            try { tran.Rollback(); } catch { /* ignore */ }
        }

        #region 同步方法

        /// <summary>
        /// Executes a database command.
        /// </summary>
        /// <param name="command">The database command specification.</param>
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
        /// Executes a database command using the specified <see cref="DbTransaction"/> on an external connection.
        /// Use this overload when you need explicit transaction control; the command is bound to the given transaction.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="transaction">The required database transaction; the command is bound to this transaction.</param>
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
        /// Executes multiple database commands as a batch; rolls back the transaction and throws on any failure.
        /// </summary>
        /// <param name="batch">The batch command specification.</param>
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
                            // Any command failure: roll back and throw with the command index
                            TryRollbackQuiet(tran);
                            throw new InvalidOperationException(
                                $"Failed to execute batch at index {i}: {spec.Kind}.", ex);
                        }
                    }

                    // Commit only after all commands succeed
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
        /// Executes a NonQuery database command and returns the number of rows affected.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">An optional transaction; pass null for no transaction.</param>
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
        /// Executes a Scalar database command and returns the single result value.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">An optional transaction; pass null for no transaction.</param>
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
        /// Executes a DataTable database command and returns the result set.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">An optional transaction; pass null for no transaction.</param>
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
        /// Executes a database command and maps each result row to an instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The target mapping type.</typeparam>
        /// <param name="command">The database command specification.</param>
        /// <returns>A <see cref="List{T}"/> containing the mapped results.</returns>
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
        /// Writes DataTable changes back to the database.
        /// </summary>
        /// <param name="spec">The DataTable update specification containing the table and its three command specifications.</param>
        /// <returns>The number of rows affected.</returns>
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
        /// Executes a SQL statement and returns the number of rows affected.
        /// </summary>
        /// <param name="commandText">The SQL statement to execute; use {0}, {1} positional placeholders.</param>
        /// <param name="values">Positional parameter values corresponding to {0}, {1}, ...</param>
        /// <returns>The number of rows affected.</returns>
        public int ExecuteNonQuery(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, commandText, values);
            return Execute(spec).RowsAffected;
        }

        /// <summary>
        /// Executes a SQL statement and returns a single scalar value.
        /// </summary>
        /// <param name="commandText">The SQL statement to execute; use {0}, {1} positional placeholders.</param>
        /// <param name="values">Positional parameter values corresponding to {0}, {1}, ...</param>
        /// <returns>The first column value of the first result row.</returns>
        public object ExecuteScalar(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar, commandText, values);
            return Execute(spec).Scalar;
        }

        /// <summary>
        /// Executes a SQL statement and returns the result as a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="commandText">The SQL statement to execute; use {0}, {1} positional placeholders.</param>
        /// <param name="values">Positional parameter values corresponding to {0}, {1}, ...</param>
        /// <returns>The query result as a <see cref="DataTable"/>.</returns>
        public DataTable ExecuteDataTable(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.DataTable, commandText, values);
            return Execute(spec).Table;
        }

        #endregion

        #region 非同步方法

        /// <summary>
        /// Asynchronously executes a database command.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="cancellationToken">A cancellation token for cancelling long-running commands.</param>
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
        /// Asynchronously executes a database command using the specified <see cref="DbTransaction"/> on an external connection.
        /// Use this overload when you need explicit transaction control; the command is bound to the given transaction.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="transaction">The required database transaction; the command is bound to this transaction.</param>
        /// <param name="cancellationToken">A cancellation token for cancelling long-running commands.</param>
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
        /// Asynchronously executes multiple database commands as a batch; rolls back the transaction and throws on any failure.
        /// </summary>
        /// <param name="batch">The batch command specification.</param>
        /// <param name="cancellationToken">A cancellation token for cancelling long-running commands.</param>
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
                        // .NET 8.0+ supports BeginTransactionAsync
                        tran = batch.IsolationLevel.HasValue
                            ? await scope.Connection.BeginTransactionAsync(batch.IsolationLevel.Value, cancellationToken).ConfigureAwait(false)
                            : await scope.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
#else
                        // .NET Standard 2.0 does not have BeginTransactionAsync; fall back to synchronous BeginTransaction
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
                            // Any command failure: roll back and throw with the command index
                            TryRollbackQuiet(tran);
                            throw new InvalidOperationException(
                                $"Failed to execute batch at index {i}: {spec.Kind}.", ex);
                        }
                    }

                    // Commit only after all commands succeed
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
        /// Asynchronously executes a NonQuery database command and returns the number of rows affected.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">An optional transaction; pass null for no transaction.</param>
        /// <param name="cancellationToken">A cancellation token for cancelling long-running commands.</param>
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
        /// Asynchronously executes a Scalar database command and returns the single result value.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">An optional transaction; pass null for no transaction.</param>
        /// <param name="cancellationToken">A cancellation token for cancelling long-running commands.</param>
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
        /// Asynchronously executes a DataTable database command and returns the result set.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">An optional transaction; pass null for no transaction.</param>
        /// <param name="cancellationToken">A cancellation token for cancelling long-running commands.</param>
        private async Task<DbCommandResult> ExecuteDataTableCoreAsync(
            DbCommandSpec command, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                if (transaction != null) cmd.Transaction = transaction; // Set the transaction first

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
        /// Asynchronously executes a database command and maps each result row to an instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The target mapping type.</typeparam>
        /// <param name="command">The database command specification.</param>
        /// <param name="cancellationToken">A cancellation token for cancelling long-running commands.</param>
        /// <returns>A <see cref="List{T}"/> containing the mapped results.</returns>
        public async Task<List<T>> QueryAsync<T>(DbCommandSpec command, CancellationToken cancellationToken = default)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            using (var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false))
            using (var cmd = command.CreateCommand(DatabaseType, scope.Connection))
            using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                var list = new List<T>();
                var mapper = ILMapper<T>.CreateMapFunc(reader); // Build a mapper based on the current column set
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
        /// Asynchronously executes a SQL statement and returns the number of rows affected.
        /// </summary>
        /// <param name="commandText">The SQL statement to execute; use {0}, {1} positional placeholders.</param>
        /// <param name="values">Positional parameter values corresponding to {0}, {1}, ...</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> ExecuteNonQueryAsync(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.NonQuery, commandText, values);
            return (await ExecuteAsync(spec)).RowsAffected;
        }

        /// <summary>
        /// Asynchronously executes a SQL statement and returns a single scalar value.
        /// </summary>
        /// <param name="commandText">The SQL statement to execute; use {0}, {1} positional placeholders.</param>
        /// <param name="values">Positional parameter values corresponding to {0}, {1}, ...</param>
        /// <returns>The first column value of the first result row.</returns>
        public async Task<object> ExecuteScalarAsync(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar, commandText, values);
            return (await ExecuteAsync(spec)).Scalar;
        }

        /// <summary>
        /// Asynchronously executes a SQL statement and returns the result as a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="commandText">The SQL statement to execute; use {0}, {1} positional placeholders.</param>
        /// <param name="values">Positional parameter values corresponding to {0}, {1}, ...</param>
        /// <returns>The query result as a <see cref="DataTable"/>.</returns>
        public async Task<DataTable> ExecuteDataTableAsync(string commandText, params object[] values)
        {
            var spec = new DbCommandSpec(DbCommandKind.DataTable, commandText, values);
            return (await ExecuteAsync(spec)).Table;
        }

        #endregion

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"DbAccess {{ DatabaseType = {DatabaseType}, Provider = {Provider?.GetType().Name} }}";
        }
    }
}
