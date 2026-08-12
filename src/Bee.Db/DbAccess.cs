using System.Data;
using System.Data.Common;
using Bee.Base.Data;

using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Logging;

namespace Bee.Db
{
    /// <summary>
    /// Provides database access operations including query execution, batch commands, and DataTable updates.
    /// </summary>
    public partial class DbAccess
    {
        private const int DefaultCommandTimeout = 30;
        private const int MaxLoggedMessageLength = 1000;
        private readonly DbConnection? _externalConnection = null;
        private readonly string _connectionString = string.Empty;
        private readonly int _maxCommandTimeout;
        private readonly string _databaseId = string.Empty;
        private readonly IAuditLogWriter? _anomalyWriter;
        private readonly DbAccessAnomalyLogOptions? _anomalyOptions;

        #region Constructors

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

        #region Sync methods

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
                table.NormalizeDateTimeMode();
                ApplyDateColumns(command, table);
                return DbCommandResult.ForTable(table);
            }
        }

        /// <summary>
        /// Marks the columns declared in <see cref="DbCommandSpec.DateColumns"/> as calendar-day columns.
        /// </summary>
        /// <param name="command">The database command specification.</param>
        /// <param name="table">The table just built from the result set.</param>
        /// <remarks>
        /// Called after `LowercaseColumnNames` so the declared names match the canonical lowercase form.
        /// Declaring the option on a kind that returns no table is rejected earlier, in
        /// <see cref="DbCommandSpec.CreateCommand"/>.
        /// </remarks>
        private static void ApplyDateColumns(DbCommandSpec command, DataTable table)
        {
            if (command.DateColumns.Count == 0) { return; }
            table.SetDateColumns([.. command.DateColumns]);
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

        #endregion

        #region Sync convenience overloads

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

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"DbAccess {{ DatabaseType = {DatabaseType}, Provider = {Provider?.GetType().Name} }}";
        }
    }
}
