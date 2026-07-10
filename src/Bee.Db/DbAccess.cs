using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Bee.Base.Data;

using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Logging;

namespace Bee.Db
{
    /// <summary>
    /// Provides database access operations including query execution, batch commands, and DataTable updates.
    /// </summary>
    public class DbAccess
    {
        private const int DefaultCommandTimeout = 30;
        private const int MaxLoggedMessageLength = 1000;
        private readonly DbConnection? _externalConnection = null;
        private readonly string _connectionString = string.Empty;
        private readonly int _maxCommandTimeout;
        private readonly string _databaseId = string.Empty;
        private readonly IAuditLogWriter? _anomalyWriter;
        private readonly DbAccessAnomalyLogOptions? _anomalyOptions;

        #region 建構函式

        /// <summary>
        /// Initializes a new instance of <see cref="DbAccess"/> for the specified database identifier.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        /// <param name="connectionManager">
        /// The DI-resolved connection manager that supplies <see cref="DbConnectionInfo"/>
        /// for <paramref name="databaseId"/>. Typically obtained via
        /// <see cref="IDbAccessFactory.Create(string)"/>; direct construction is permitted
        /// when callers already hold an injected manager.
        /// </param>
        /// <param name="maxCommandTimeout">
        /// Per-app upper bound applied to each <see cref="DbCommand.CommandTimeout"/>;
        /// 0 (default) disables the cap, in which case the value supplied via
        /// <see cref="DbCommandSpec.CommandTimeout"/> is used as-is.
        /// Typically supplied by <see cref="IDbAccessFactory"/> at the host level
        /// (e.g. 30 sec for mobile API, 60 sec for web, 120 sec for batch service).
        /// </param>
        /// <param name="anomalyWriter">
        /// Optional audit writer for DB anomaly records (Error / Timeout / Slow / large-row);
        /// null disables DB anomaly logging.
        /// </param>
        /// <param name="anomalyOptions">Optional DB anomaly thresholds and level.</param>
        public DbAccess(string databaseId, IDbConnectionManager connectionManager, int maxCommandTimeout = 0,
            IAuditLogWriter? anomalyWriter = null, DbAccessAnomalyLogOptions? anomalyOptions = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            ArgumentNullException.ThrowIfNull(connectionManager);

            var connInfo = connectionManager.GetConnectionInfo(databaseId);

            DatabaseType = connInfo.DatabaseType;
            Provider = connInfo.Provider;
            _connectionString = connInfo.ConnectionString;
            _maxCommandTimeout = maxCommandTimeout;
            _databaseId = databaseId;
            _anomalyWriter = anomalyWriter;
            _anomalyOptions = anomalyOptions;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DbAccess"/> using an externally managed <see cref="DbConnection"/>.
        /// The connection lifetime is managed by the caller.
        /// </summary>
        /// <param name="externalConnection">The externally provided database connection.</param>
        /// <param name="databaseType">The database type of the external connection.</param>
        /// <param name="maxCommandTimeout">
        /// Per-app upper bound applied to each <see cref="DbCommand.CommandTimeout"/>;
        /// 0 (default) disables the cap. See the other constructor overload for details.
        /// </param>
        public DbAccess(DbConnection externalConnection, DatabaseType databaseType, int maxCommandTimeout = 0)
        {
            _externalConnection = externalConnection ?? throw new ArgumentNullException(nameof(externalConnection));
            DatabaseType = databaseType;
            Provider = DbProviderRegistry.Get(DatabaseType)
                ?? throw new InvalidOperationException($"Unknown database type: {DatabaseType}.");
            _maxCommandTimeout = maxCommandTimeout;
        }

        #endregion

        /// <summary>
        /// Resolves the effective <see cref="DbCommand.CommandTimeout"/> value:
        /// non-positive → <c>DefaultCommandTimeout</c> (30 sec); cap=0 → as-is;
        /// otherwise → <c>min(requested, cap)</c>.
        /// </summary>
        private int ResolveTimeout(int requested)
        {
            if (requested <= 0) return DefaultCommandTimeout;
            if (_maxCommandTimeout <= 0) return requested;
            return Math.Min(requested, _maxCommandTimeout);
        }

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
            return DbConnectionScope.Create(_externalConnection, Provider, _connectionString,
                DbProviderRegistry.GetConnectionInitializer(DatabaseType));
        }

        /// <summary>
        /// Asynchronously creates a connection scope, automatically choosing between the external connection and a newly created one.
        /// </summary>
        private Task<DbConnectionScope> CreateScopeAsync(CancellationToken cancellationToken = default)
        {
            return DbConnectionScope.CreateAsync(_externalConnection, Provider, _connectionString,
                DbProviderRegistry.GetConnectionInitializer(DatabaseType), cancellationToken);
        }

        /// <summary>
        /// Attempts to roll back a transaction, silently ignoring any exceptions during rollback.
        /// </summary>
        private static void TryRollbackQuiet(DbTransaction? tran)
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
            ArgumentNullException.ThrowIfNull(command);

            return RunWithAnomalyDetection(command, () =>
            {
                using var scope = CreateScope();
                return DispatchExecute(command, scope.Connection!, null);
            });
        }

        /// <summary>
        /// Executes a database command using the specified <see cref="DbTransaction"/> on an external connection.
        /// Use this overload when you need explicit transaction control; the command is bound to the given transaction.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="transaction">The required database transaction; the command is bound to this transaction.</param>
        public DbCommandResult Execute(DbCommandSpec command, DbTransaction transaction)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(transaction);

            var conn = transaction.Connection
                       ?? throw new InvalidOperationException("Transaction has no associated connection.");

            return RunWithAnomalyDetection(command, () => DispatchExecute(command, conn, transaction));
        }

        private DbCommandResult DispatchExecute(DbCommandSpec command, DbConnection connection, DbTransaction? transaction)
            => command.Kind switch
            {
                DbCommandKind.NonQuery => ExecuteNonQueryCore(command, connection, transaction),
                DbCommandKind.Scalar => ExecuteScalarCore(command, connection, transaction),
                DbCommandKind.DataTable => ExecuteDataTableCore(command, connection, transaction),
                _ => throw new NotSupportedException($"Unsupported DbCommandKind: {command.Kind}."),
            };

        /// <summary>
        /// Runs <paramref name="exec"/>, detecting and recording anomalies (Error / Timeout on
        /// failure, Slow / LargeAffected / LargeResult on success). A no-op wrapper when anomaly
        /// logging is disabled. Anomaly writes are best-effort and never alter the command outcome.
        /// </summary>
        private DbCommandResult RunWithAnomalyDetection(DbCommandSpec command, Func<DbCommandResult> exec)
        {
            if (_anomalyWriter == null || _anomalyOptions == null
                || _anomalyOptions.Level == DbAccessAnomalyLogLevel.None)
            {
                return exec();
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = exec();
                stopwatch.Stop();
                LogSuccessAnomalies(command, result, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (DbException ex)
            {
                stopwatch.Stop();
                LogFailureAnomaly(command, ex, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private void LogSuccessAnomalies(DbCommandSpec command, DbCommandResult result, long elapsedMs)
        {
            // Slow / large-row are "abnormal but succeeded" — only recorded at Warning level.
            if (_anomalyOptions!.Level != DbAccessAnomalyLogLevel.Warning) { return; }

            int slowThresholdMs = _anomalyOptions.ExecutionTimeThreshold > 0
                ? _anomalyOptions.ExecutionTimeThreshold * 1000 : 0;
            if (slowThresholdMs > 0 && elapsedMs > slowThresholdMs)
                WriteDbAnomaly(command, AnomalyKind.Slow, elapsedMs, thresholdMs: slowThresholdMs);

            if (_anomalyOptions.AffectedRowThreshold > 0 && result.RowsAffected > _anomalyOptions.AffectedRowThreshold)
                WriteDbAnomaly(command, AnomalyKind.LargeAffected, elapsedMs, affectedRows: result.RowsAffected);

            int resultRows = result.Table?.Rows.Count ?? 0;
            if (_anomalyOptions.ResultRowThreshold > 0 && resultRows > _anomalyOptions.ResultRowThreshold)
                WriteDbAnomaly(command, AnomalyKind.LargeResult, elapsedMs, resultRows: resultRows);
        }

        private void LogFailureAnomaly(DbCommandSpec command, DbException ex, long elapsedMs)
        {
            var kind = IsTimeout(ex, elapsedMs, command) ? AnomalyKind.Timeout : AnomalyKind.Error;
            WriteDbAnomaly(command, kind, elapsedMs,
                errorType: ex.GetType().Name, errorMessage: SanitizeMessage(ex.Message));
        }

        private bool IsTimeout(DbException ex, long elapsedMs, DbCommandSpec command)
        {
            if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)) { return true; }
            int timeoutSec = ResolveTimeout(command.CommandTimeout);
            return timeoutSec > 0 && elapsedMs >= (long)timeoutSec * 1000 * 9 / 10;
        }

        private void WriteDbAnomaly(DbCommandSpec command, AnomalyKind kind, long elapsedMs,
            int? thresholdMs = null, int? affectedRows = null, int? resultRows = null,
            string? errorType = null, string? errorMessage = null)
        {
            _anomalyWriter!.Write(new DbAnomalyEntry
            {
                DatabaseId = _databaseId,
                Command = command.CommandText,   // {0} template only — never the parameter values
                Kind = kind,
                ElapsedMs = elapsedMs > int.MaxValue ? int.MaxValue : (int)elapsedMs,
                ThresholdMs = thresholdMs,
                AffectedRows = affectedRows,
                ResultRows = resultRows,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
            });
        }

        private static string SanitizeMessage(string message)
        {
            // Provider error text only (no stack trace, no parameter values); flattened and capped.
            var oneLine = message.Replace('\r', ' ').Replace('\n', ' ');
            return oneLine.Length <= MaxLoggedMessageLength ? oneLine : oneLine[..MaxLoggedMessageLength];
        }

        /// <summary>
        /// Executes multiple database commands as a batch; rolls back the transaction and throws on any failure.
        /// </summary>
        /// <param name="batch">The batch command specification.</param>
        public DbBatchResult ExecuteBatch(DbBatchSpec batch)
        {
            ArgumentNullException.ThrowIfNull(batch);
            if (batch.Commands == null) throw new ArgumentException("batch.Commands cannot be null.", nameof(batch));
            if (batch.Commands.Count == 0) throw new ArgumentException("Batch contains no commands.", nameof(batch));

            var result = new DbBatchResult();

            using (var scope = CreateScope())
            {
                DbTransaction? tran = null;

                try
                {
                    if (batch.UseTransaction)
                    {
                        tran = batch.IsolationLevel.HasValue
                            ? scope.Connection!.BeginTransaction(batch.IsolationLevel.Value)
                            : scope.Connection!.BeginTransaction();
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
                                    item = ExecuteNonQueryCore(spec, scope.Connection!, tran);
                                    result.RowsAffectedSum += item.RowsAffected;
                                    break;
                                case DbCommandKind.Scalar:
                                    item = ExecuteScalarCore(spec, scope.Connection!, tran);
                                    break;
                                case DbCommandKind.DataTable:
                                    item = ExecuteDataTableCore(spec, scope.Connection!, tran);
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
            DbCommandSpec command, DbConnection connection, DbTransaction? transaction)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                cmd.CommandTimeout = ResolveTimeout(command.CommandTimeout);
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
            DbCommandSpec command, DbConnection connection, DbTransaction? transaction)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                cmd.CommandTimeout = ResolveTimeout(command.CommandTimeout);
                if (transaction != null) cmd.Transaction = transaction;
                var value = cmd.ExecuteScalar();
                return DbCommandResult.ForScalar(value);
            }
        }

        /// <summary>
        /// Executes a DataTable database command and returns the result set.
        /// </summary>
        /// <remarks>
        /// Design note: <c>adapter.Fill</c> loads the entire result set into memory.
        /// For large result sets, prefer <see cref="Query{T}"/> which streams rows via
        /// <see cref="System.Data.Common.DbDataReader"/>.
        /// </remarks>
        /// <param name="command">The database command specification.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">An optional transaction; pass null for no transaction.</param>
        private DbCommandResult ExecuteDataTableCore(
            DbCommandSpec command, DbConnection connection, DbTransaction? transaction)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                cmd.CommandTimeout = ResolveTimeout(command.CommandTimeout);
                if (transaction != null) cmd.Transaction = transaction;

                // Every registered provider supplies a DbDataAdapter — SQLite via the framework's
                // SqliteProviderFactory wrapper — so the sync read uses Fill uniformly. (The async
                // overload cannot: DbDataAdapter has no FillAsync, so it streams via DbDataReader.)
                var adapter = Provider.CreateDataAdapter()
                    ?? throw new InvalidOperationException(
                        $"Provider for {DatabaseType} supplies no DbDataAdapter; register " +
                        "SqliteProviderFactory for SQLite.");
                var table = new DataTable("DataTable");
                using (adapter)
                {
                    adapter.SelectCommand = cmd;
                    adapter.Fill(table);
                }
                table.LowercaseColumnNames();
                return DbCommandResult.ForTable(table);
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
            ArgumentNullException.ThrowIfNull(command);

            using (var scope = CreateScope())
            using (var cmd = command.CreateCommand(DatabaseType, scope.Connection!))
            {
                cmd.CommandTimeout = ResolveTimeout(command.CommandTimeout);
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
        }

        /// <summary>
        /// Writes DataTable changes back to the database.
        /// </summary>
        /// <param name="spec">The DataTable update specification containing the table and its three command specifications.</param>
        /// <returns>The number of rows affected.</returns>
        public int UpdateDataTable(DataTableUpdateSpec spec)
        {
            ValidateUpdateSpec(spec);

            using (var scope = CreateScope())
            {
                DbTransaction? tran = null;
                try
                {
                    tran = BeginTransactionIfRequested(scope, spec);
                    int affected = ApplySpec(spec, scope.Connection!, tran);
                    tran?.Commit();
                    return affected;
                }
                catch
                {
                    TryRollbackQuiet(tran);
                    throw;
                }
                finally
                {
                    tran?.Dispose();
                }
            }
        }

        /// <summary>
        /// Applies one update spec on the given connection/transaction through a provider
        /// <see cref="DbDataAdapter"/>. Every registered provider supplies one — SQLite via the
        /// framework's <see cref="Bee.Db.Providers.Sqlite.SqliteProviderFactory"/> wrapper — so a
        /// null adapter means the host registered the raw Sqlite factory by mistake and is reported
        /// as a configuration error.
        /// </summary>
        private int ApplySpec(DataTableUpdateSpec spec, DbConnection connection, DbTransaction? tran)
        {
            DbCommand? insert = spec.InsertCommand?.CreateCommand(DatabaseType, connection);
            DbCommand? update = spec.UpdateCommand?.CreateCommand(DatabaseType, connection);
            DbCommand? delete = spec.DeleteCommand?.CreateCommand(DatabaseType, connection);
            // The framework re-fetches saved rows via GetData, so the adapter must not try to read
            // results back into the DataTable after each command.
            if (insert != null) { insert.CommandTimeout = ResolveTimeout(spec.InsertCommand!.CommandTimeout); insert.UpdatedRowSource = UpdateRowSource.None; }
            if (update != null) { update.CommandTimeout = ResolveTimeout(spec.UpdateCommand!.CommandTimeout); update.UpdatedRowSource = UpdateRowSource.None; }
            if (delete != null) { delete.CommandTimeout = ResolveTimeout(spec.DeleteCommand!.CommandTimeout); delete.UpdatedRowSource = UpdateRowSource.None; }
            AttachTransaction(tran, insert, update, delete);

            try
            {
                var adapter = Provider.CreateDataAdapter()
                    ?? throw new InvalidOperationException(
                        $"Provider for {DatabaseType} supplies no DbDataAdapter; register " +
                        "SqliteProviderFactory for SQLite.");
                using (adapter)
                {
                    adapter.InsertCommand = insert;
                    adapter.UpdateCommand = update;
                    adapter.DeleteCommand = delete;
                    return adapter.Update(spec.DataTable);
                }
            }
            finally
            {
                insert?.Dispose();
                update?.Dispose();
                delete?.Dispose();
            }
        }

        /// <summary>
        /// Writes changes from several DataTables back to the database inside a single
        /// transaction. Each spec is applied in list order through a DataAdapter, so the
        /// caller supplies parent-before-child order for insert FK correctness. Either every
        /// spec commits, or any failure rolls the whole batch back.
        /// </summary>
        /// <param name="specs">The per-table update specifications, in execution order.</param>
        /// <returns>Rows affected per spec, aligned with the input order.</returns>
        public IReadOnlyList<int> UpdateDataTables(IReadOnlyList<DataTableUpdateSpec> specs)
        {
            ArgumentNullException.ThrowIfNull(specs);
            if (specs.Count == 0) return Array.Empty<int>();
            foreach (var spec in specs) ValidateUpdateSpec(spec);

            using (var scope = CreateScope())
            {
                DbTransaction? tran = null;
                try
                {
                    tran = scope.Connection!.BeginTransaction();
                    var affected = new int[specs.Count];
                    for (int i = 0; i < specs.Count; i++)
                        affected[i] = ApplySpec(specs[i], scope.Connection!, tran);

                    tran.Commit();
                    return affected;
                }
                catch
                {
                    TryRollbackQuiet(tran);
                    throw;
                }
                finally
                {
                    tran?.Dispose();
                }
            }
        }

        private static void ValidateUpdateSpec(DataTableUpdateSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);
            if (spec.DataTable == null) throw new ArgumentException("spec.DataTable cannot be null.", nameof(spec));
            if (spec.InsertCommand == null && spec.UpdateCommand == null && spec.DeleteCommand == null)
                throw new ArgumentException("At least one of Insert/Update/Delete command spec must be provided.", nameof(spec));
        }

        private static DbTransaction? BeginTransactionIfRequested(DbConnectionScope scope, DataTableUpdateSpec spec)
        {
            if (!spec.UseTransaction) return null;
            return spec.IsolationLevel.HasValue
                ? scope.Connection!.BeginTransaction(spec.IsolationLevel.Value)
                : scope.Connection!.BeginTransaction();
        }

        private static void AttachTransaction(DbTransaction? tran, DbCommand? insert, DbCommand? update, DbCommand? delete)
        {
            if (tran == null) return;
            if (insert != null) insert.Transaction = tran;
            if (update != null) update.Transaction = tran;
            if (delete != null) delete.Transaction = tran;
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
        public object? ExecuteScalar(string commandText, params object[] values)
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
        public DataTable? ExecuteDataTable(string commandText, params object[] values)
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
            ArgumentNullException.ThrowIfNull(command);

            using (var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false))
            {
                switch (command.Kind)
                {
                    case DbCommandKind.NonQuery:
                        return await ExecuteNonQueryCoreAsync(command, scope.Connection!, null, cancellationToken).ConfigureAwait(false);
                    case DbCommandKind.Scalar:
                        return await ExecuteScalarCoreAsync(command, scope.Connection!, null, cancellationToken).ConfigureAwait(false);
                    case DbCommandKind.DataTable:
                        return await ExecuteDataTableCoreAsync(command, scope.Connection!, null, cancellationToken).ConfigureAwait(false);
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
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(transaction);

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
            ArgumentNullException.ThrowIfNull(batch);
            if (batch.Commands == null) throw new ArgumentException("batch.Commands cannot be null.", nameof(batch));
            if (batch.Commands.Count == 0) throw new ArgumentException("Batch contains no commands.", nameof(batch));

            var result = new DbBatchResult();

            using (var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false))
            {
                DbTransaction? tran = null;

                try
                {
                    if (batch.UseTransaction)
                    {
                        tran = batch.IsolationLevel.HasValue
                            ? await scope.Connection!.BeginTransactionAsync(batch.IsolationLevel.Value, cancellationToken).ConfigureAwait(false)
                            : await scope.Connection!.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
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
                                    item = await ExecuteNonQueryCoreAsync(spec, scope.Connection!, tran, cancellationToken).ConfigureAwait(false);
                                    result.RowsAffectedSum += item.RowsAffected;
                                    break;
                                case DbCommandKind.Scalar:
                                    item = await ExecuteScalarCoreAsync(spec, scope.Connection!, tran, cancellationToken).ConfigureAwait(false);
                                    break;
                                case DbCommandKind.DataTable:
                                    item = await ExecuteDataTableCoreAsync(spec, scope.Connection!, tran, cancellationToken).ConfigureAwait(false);
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
                    try
                    {
                        if (tran != null)
                            await tran.CommitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to commit transaction.", ex);
                    }
                }
                finally
                {
                    if (tran != null) await tran.DisposeAsync().ConfigureAwait(false);
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
            DbCommandSpec command, DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                cmd.CommandTimeout = ResolveTimeout(command.CommandTimeout);
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
            DbCommandSpec command, DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                cmd.CommandTimeout = ResolveTimeout(command.CommandTimeout);
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
            DbCommandSpec command, DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken)
        {
            using (var cmd = command.CreateCommand(DatabaseType, connection))
            {
                cmd.CommandTimeout = ResolveTimeout(command.CommandTimeout);
                if (transaction != null) cmd.Transaction = transaction; // Set the transaction first

                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    var table = new DataTable("DataTable");
                    table.Load(reader);
                    table.LowercaseColumnNames();
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
            ArgumentNullException.ThrowIfNull(command);

            using (var scope = await CreateScopeAsync(cancellationToken).ConfigureAwait(false))
            using (var cmd = command.CreateCommand(DatabaseType, scope.Connection!))
            {
                cmd.CommandTimeout = ResolveTimeout(command.CommandTimeout);
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
        public async Task<object?> ExecuteScalarAsync(string commandText, params object[] values)
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
        public async Task<DataTable?> ExecuteDataTableAsync(string commandText, params object[] values)
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
