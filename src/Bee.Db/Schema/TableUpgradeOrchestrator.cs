using Bee.Base;
using Bee.Db.Ddl;
using Bee.Db.Manager;
using Bee.Db.Providers;
using Bee.Db.Schema.Changes;

namespace Bee.Db.Schema
{
    /// <summary>
    /// Converts a <see cref="TableSchemaDiff"/> into an <see cref="UpgradePlan"/> and executes it.
    /// Aggregation rules: all ALTER-capable → ALTER path; any rebuild-required → full rebuild;
    /// any NotSupported or rebuild-with-rename → throw.
    /// </summary>
    public class TableUpgradeOrchestrator
    {
        private readonly IDialectFactory _dialect;
        private readonly IDbConnectionManager _connectionManager;
        private readonly ITableAlterCommandBuilder _alterBuilder;

        /// <summary>
        /// Initializes a new instance of <see cref="TableUpgradeOrchestrator"/> for the specified database,
        /// resolving the dialect factory from <see cref="DbDialectRegistry"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier used to resolve the dialect factory.</param>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public TableUpgradeOrchestrator(string databaseId, IDbConnectionManager connectionManager)
            : this(ResolveDialect(databaseId, connectionManager), connectionManager)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="TableUpgradeOrchestrator"/> with the supplied dialect factory.
        /// </summary>
        /// <param name="dialect">The dialect factory for the target database.</param>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public TableUpgradeOrchestrator(IDialectFactory dialect, IDbConnectionManager connectionManager)
        {
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _alterBuilder = _dialect.CreateTableAlterCommandBuilder();
        }

        private static IDialectFactory ResolveDialect(string databaseId, IDbConnectionManager connectionManager)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            ArgumentNullException.ThrowIfNull(connectionManager);
            var connInfo = connectionManager.GetConnectionInfo(databaseId);
            return DbDialectRegistry.Get(connInfo.DatabaseType);
        }

        /// <summary>
        /// Builds an <see cref="UpgradePlan"/> for the given diff. Does not execute SQL.
        /// </summary>
        /// <param name="diff">The schema diff.</param>
        /// <param name="options">Upgrade options; null uses <see cref="UpgradeOptions.Default"/>.</param>
        public UpgradePlan Plan(TableSchemaDiff diff, UpgradeOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(diff);
            options ??= UpgradeOptions.Default;

            if (diff.IsNewTable)
                return BuildCreatePlan(diff);

            if (diff.IsEmpty)
                return new UpgradePlan(UpgradeExecutionMode.NoChange);

            bool needsRebuild = false;
            foreach (var change in diff.Changes)
            {
                var kind = _alterBuilder.GetExecutionKind(change);
                if (kind == ChangeExecutionKind.NotSupported)
                    throw new InvalidOperationException($"Change is not supported by the current provider: {change.GetType().Name}.");
                if (kind == ChangeExecutionKind.Rebuild)
                    needsRebuild = true;
            }

            if (needsRebuild)
            {
                bool hasRename = diff.Changes.OfType<RenameFieldChange>().Any();
                if (hasRename)
                    throw new InvalidOperationException("Rebuild combined with a field rename is not supported; split the changes across deploys or drop the OriginalFieldName hint.");
                return BuildRebuildPlan(diff);
            }

            return BuildAlterPlan(diff, options);
        }

        /// <summary>
        /// Executes the plan against the specified database, running each stage in its own transaction.
        /// Returns <c>true</c> if any stage ran; <c>false</c> for an empty plan.
        /// </summary>
        /// <param name="plan">The plan to execute.</param>
        /// <param name="databaseId">The database identifier to open connections for.</param>
        public bool Execute(UpgradePlan plan, string databaseId)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            if (plan.IsEmpty) return false;

            var databaseType = _connectionManager.GetConnectionInfo(databaseId).DatabaseType;

            foreach (var stage in plan.Stages)
            {
                using var conn = _connectionManager.CreateConnection(databaseId);
                conn.Open();
                using var txn = conn.BeginTransaction();
                try
                {
                    var stagedAccess = new DbAccess(conn, databaseType);
                    foreach (var sql in stage.Statements)
                    {
                        if (StringUtilities.IsEmpty(sql)) continue;
                        var cmd = new DbCommandSpec(DbCommandKind.NonQuery, sql);
                        stagedAccess.Execute(cmd, txn);
                    }
                    txn.Commit();
                }
                catch
                {
                    try { txn.Rollback(); } catch { /* ignore rollback errors */ }
                    throw;
                }
            }
            return true;
        }

        private UpgradePlan BuildCreatePlan(TableSchemaDiff diff)
        {
            var builder = _dialect.CreateCreateTableCommandBuilder();
            var sql = builder.GetCommandText(diff.DefineTable);
            var stage = new UpgradeStage(UpgradeStageKind.CreateTable, new[] { sql });
            return new UpgradePlan(UpgradeExecutionMode.Create, new[] { stage });
        }

        private UpgradePlan BuildRebuildPlan(TableSchemaDiff diff)
        {
            var builder = _dialect.CreateTableRebuildCommandBuilder();
            var sql = builder.GetCommandText(diff);
            var stage = new UpgradeStage(UpgradeStageKind.Rebuild, new[] { sql });
            return new UpgradePlan(UpgradeExecutionMode.Rebuild, new[] { stage });
        }

        private UpgradePlan BuildAlterPlan(TableSchemaDiff diff, UpgradeOptions options)
        {
            string tableName = diff.DefineTable.TableName;
            var dropIndexStmts = new List<string>();
            var alterColumnStmts = new List<string>();
            var addColumnStmts = new List<string>();
            var createIndexStmts = new List<string>();
            var warnings = new List<string>();

            foreach (var change in diff.Changes)
            {
                if (_alterBuilder.IsNarrowingChange(change))
                {
                    if (!options.AllowColumnNarrowing)
                        throw new InvalidOperationException(
                            $"Change narrows a column ({change.GetType().Name}); set UpgradeOptions.AllowColumnNarrowing to proceed.");
                    warnings.Add($"Narrowing change permitted: {change.Describe()}");
                }

                var stmts = _alterBuilder.GetStatements(tableName, change);
                switch (change)
                {
                    case DropIndexChange _:
                        dropIndexStmts.AddRange(stmts);
                        break;
                    case RenameFieldChange _:
                    case AlterFieldChange _:
                        alterColumnStmts.AddRange(stmts);
                        break;
                    case AddFieldChange _:
                        addColumnStmts.AddRange(stmts);
                        break;
                    case AddIndexChange _:
                        createIndexStmts.AddRange(stmts);
                        break;
                    default:
                        throw new InvalidOperationException($"Unrecognized change type: {change.GetType().Name}");
                }
            }

            var stages = new List<UpgradeStage>();
            if (dropIndexStmts.Count > 0) stages.Add(new UpgradeStage(UpgradeStageKind.DropIndexes, dropIndexStmts));
            if (alterColumnStmts.Count > 0) stages.Add(new UpgradeStage(UpgradeStageKind.AlterColumns, alterColumnStmts));
            if (addColumnStmts.Count > 0) stages.Add(new UpgradeStage(UpgradeStageKind.AddColumns, addColumnStmts));
            if (createIndexStmts.Count > 0) stages.Add(new UpgradeStage(UpgradeStageKind.CreateIndexes, createIndexStmts));

            AppendDescriptionSyncStage(stages, diff);

            return new UpgradePlan(UpgradeExecutionMode.Alter, stages, warnings);
        }

        /// <summary>
        /// Appends a description-sync stage to <paramref name="stages"/> when the active dialect can
        /// persist descriptions and there is something to apply.
        /// </summary>
        /// <remarks>
        /// The sync runs last, after the columns it describes exist. A dialect that cannot store
        /// descriptions at all returns no builder and the stage is skipped, which is why the seam is
        /// <see cref="IDialectFactory.CreateDescriptionSyncCommandBuilder"/> returning null rather
        /// than a builder that yields nothing: the distinction is "this dialect has no such facility",
        /// not "nothing changed".
        /// </remarks>
        private void AppendDescriptionSyncStage(List<UpgradeStage> stages, TableSchemaDiff diff)
        {
            var builder = _dialect.CreateDescriptionSyncCommandBuilder();
            if (builder == null) return;

            var statements = builder.GetStatements(diff);
            if (statements.Count > 0)
                stages.Add(new UpgradeStage(UpgradeStageKind.SyncDescriptions, statements));
        }

    }
}
