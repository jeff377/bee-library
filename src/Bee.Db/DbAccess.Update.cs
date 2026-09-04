using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;


namespace Bee.Db
{
    /// <summary>
    /// Writing `DataTable` changes back to the database, single-table and multi-table.
    /// </summary>
    /// <remarks>
    /// Its own concern: the three per-row commands, the shared transaction across tables, and the
    /// ordering that keeps foreign keys satisfied. None of it is reachable from the query path.
    /// </remarks>
    public partial class DbAccess
    {
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
                    TryEnableBatching(adapter);
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
        /// Rows sent to the server per round trip when the provider can batch.
        /// </summary>
        /// <remarks>
        /// Bounded rather than <c>0</c> ("as many as the provider will take"). The win is almost all
        /// in getting off one-round-trip-per-row. Measured through <see cref="UpdateDataTables"/>
        /// itself against a local SQL Server container (median of three): 100 rows 32 ms → 3 ms,
        /// 500 rows 145 ms → 9 ms. The cost was the trips, not the SQL — and a remote database only
        /// widens that gap. A bound keeps a pathologically large save from assembling one enormous
        /// command, while at this size a typical form save is already a single trip.
        /// </remarks>
        private const int UpdateBatchRows = 100;

        /// <summary>
        /// Adapter types already known to accept or reject <see cref="DbDataAdapter.UpdateBatchSize"/>.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, bool> s_supportsBatching = new();

        /// <summary>
        /// Asks the adapter to batch its per-row commands, and leaves it alone when it cannot.
        /// </summary>
        /// <param name="adapter">The adapter about to run <see cref="DbDataAdapter.Update(DataTable)"/>.</param>
        /// <remarks>
        /// <para>
        /// ADO.NET defaults <see cref="DbDataAdapter.UpdateBatchSize"/> to <c>1</c> — one
        /// <c>ExecuteNonQuery</c>, and therefore one round trip, for <b>every changed row</b>. The
        /// framework had never set it, so a save's cost was dominated by trips to a machine that is
        /// usually not this one.
        /// </para>
        /// <para>
        /// Support is detected rather than listed, because a hard-coded provider list would be wrong
        /// in both directions: it drifts as providers gain support, and it cannot know which factory
        /// a host actually registered for a given <see cref="DatabaseType"/>. <see cref="DbDataAdapter"/>'s
        /// base setter throws <see cref="NotSupportedException"/>, so asking is the check. Measured
        /// today: SQL Server, MySQL and Oracle accept it; Npgsql and the framework's own SQLite
        /// adapter throw.
        /// </para>
        /// <para>
        /// The result is cached per adapter type, so the exception is thrown at most once per type
        /// per process rather than on every save.
        /// </para>
        /// <para>
        /// WARNING: batching requires <c>UpdatedRowSource.None</c> on all three commands — an adapter
        /// cannot read per-row output back while several rows are in flight. <see cref="ApplySpec"/>
        /// sets it because the framework re-fetches saved rows through <c>GetData</c> instead; if that
        /// ever changes, this has to go with it.
        /// </para>
        /// </remarks>
        private static void TryEnableBatching(DbDataAdapter adapter)
        {
            var type = adapter.GetType();
            if (s_supportsBatching.TryGetValue(type, out bool supported))
            {
                if (supported) { adapter.UpdateBatchSize = UpdateBatchRows; }
                return;
            }

            try
            {
                adapter.UpdateBatchSize = UpdateBatchRows;
                s_supportsBatching[type] = true;
            }
            catch (NotSupportedException)
            {
                // The provider's adapter does not implement batching; one round trip per row is the
                // only thing it offers. Nothing to report — this is a capability, not a failure.
                s_supportsBatching[type] = false;
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
                        affected[i] = ApplySpec(specs[i], scope.Connection, tran);

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
    }
}
