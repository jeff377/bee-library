using System.Data;
using System.Data.Common;
using Bee.Base.Data;

namespace Bee.Db
{
    /// <summary>
    /// Asynchronous half of <see cref="DbAccess"/>. Split from the synchronous members purely for
    /// file size; behaviour and public surface are unchanged.
    /// </summary>
    public partial class DbAccess
    {
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
    }
}
