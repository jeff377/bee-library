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
