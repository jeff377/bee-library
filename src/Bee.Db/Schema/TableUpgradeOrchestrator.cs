using Bee.Base;
using Bee.Db.Ddl;
using Bee.Db.Manager;
using Bee.Db.Providers;
using Bee.Db.Providers.SqlServer;
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
        private readonly ITableAlterCommandBuilder _alterBuilder;

        /// <summary>
        /// Initializes a new instance of <see cref="TableUpgradeOrchestrator"/> for the specified database,
        /// resolving the dialect factory from <see cref="DbDialectRegistry"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier used to resolve the dialect factory.</param>
        public TableUpgradeOrchestrator(string databaseId)
            : this(ResolveDialect(databaseId))
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="TableUpgradeOrchestrator"/> with the supplied dialect factory.
        /// </summary>
        /// <param name="dialect">The dialect factory for the target database.</param>
        public TableUpgradeOrchestrator(IDialectFactory dialect)
        {
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
            _alterBuilder = _dialect.CreateTableAlterCommandBuilder();
        }

        private static IDialectFactory ResolveDialect(string databaseId)
        {
            BaseFunc.EnsureNotNullOrWhiteSpace((databaseId, nameof(databaseId)));
            var connInfo = DbConnectionManager.GetConnectionInfo(databaseId);
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
        public static bool Execute(UpgradePlan plan, string databaseId)
        {
            ArgumentNullException.ThrowIfNull(plan);
            BaseFunc.EnsureNotNullOrWhiteSpace((databaseId, nameof(databaseId)));

            if (plan.IsEmpty) return false;

            foreach (var stage in plan.Stages)
            {
                using var conn = DbFunc.CreateConnection(databaseId);
                conn.Open();
                using var txn = conn.BeginTransaction();
                try
                {
                    var stagedAccess = new DbAccess(conn);
                    foreach (var sql in stage.Statements)
                    {
                        if (StrFunc.IsEmpty(sql)) continue;
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

            if (diff.DescriptionChanges.Count > 0)
            {
                var descSql = SqlExtendedPropertyCommandBuilder.GetCommandText(tableName, diff.DescriptionChanges);
                if (StrFunc.IsNotEmpty(descSql))
                    stages.Add(new UpgradeStage(UpgradeStageKind.SyncDescriptions, new[] { descSql }));
            }

            return new UpgradePlan(UpgradeExecutionMode.Alter, stages, warnings);
        }

    }
}
