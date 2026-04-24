using System.Text;
using Bee.Base;
using Bee.Db.Manager;
using Bee.Db.Providers;
using Bee.Definition;
using Bee.Definition.Database;

namespace Bee.Db.Schema
{
    /// <summary>
    /// Compares a defined table schema against the actual database schema and produces (or executes)
    /// the required upgrade commands. Routes structural changes through <see cref="TableUpgradeOrchestrator"/>
    /// (ALTER-based strategy with rebuild fallback); metadata-only drift is handled by
    /// the provider-specific extended-property builder inside the orchestrator's description stage.
    /// </summary>
    public class TableSchemaBuilder
    {
        private readonly IDialectFactory _dialect;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="TableSchemaBuilder"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public TableSchemaBuilder(string databaseId)
        {
            BaseFunc.EnsureNotNullOrWhiteSpace((databaseId, nameof(databaseId)));
            DatabaseId = databaseId;
            var connInfo = DbConnectionManager.GetConnectionInfo(databaseId);
            _dialect = DbDialectRegistry.Get(connInfo.DatabaseType);
        }

        #endregion

        /// <summary>
        /// Gets the database identifier.
        /// </summary>
        public string DatabaseId { get; private set; }

        /// <summary>
        /// Creates a <see cref="TableSchemaComparer"/> for the specified table.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        private TableSchemaComparer CreateComparer(string dbName, string tableName)
        {
            // Actual table schema from the database
            var provider = _dialect.CreateTableSchemaProvider(this.DatabaseId);
            var realTable = provider.GetTableSchema(tableName);
            // Defined table schema from the form definitions
            var defineTable = BackendInfo.DefineAccess.GetTableSchema(dbName, tableName);
            return new TableSchemaComparer(defineTable, realTable);
        }

        /// <summary>
        /// Compares the actual table schema with the defined schema and returns the legacy comparison result
        /// (retained for existing callers; new code should use <see cref="CompareToDiff"/>).
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema Compare(string dbName, string tableName)
        {
            return CreateComparer(dbName, tableName).Compare();
        }

        /// <summary>
        /// Produces a structured <see cref="TableSchemaDiff"/> for the specified table.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchemaDiff CompareToDiff(string dbName, string tableName)
        {
            return CreateComparer(dbName, tableName).CompareToDiff();
        }

        /// <summary>
        /// Plans the upgrade for the specified table and returns the combined SQL script (all stages concatenated).
        /// Returns an empty string when no changes are required.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="options">Upgrade options; null uses <see cref="UpgradeOptions.Default"/>.</param>
        public string GetCommandText(string dbName, string tableName, UpgradeOptions? options = null)
        {
            var diff = CompareToDiff(dbName, tableName);
            var plan = new TableUpgradeOrchestrator(_dialect).Plan(diff, options);
            if (plan.IsEmpty) return string.Empty;

            var sb = new StringBuilder();
            foreach (var sql in plan.AllStatements)
            {
                if (StrFunc.IsEmpty(sql)) continue;
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(sql);
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Compares the table schema and executes the upgrade if differences are found.
        /// Each stage runs in its own transaction.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="options">Upgrade options; null uses <see cref="UpgradeOptions.Default"/>.</param>
        /// <remarks>Returns <c>true</c> if an upgrade was performed; otherwise <c>false</c>.</remarks>
        public bool Execute(string dbName, string tableName, UpgradeOptions? options = null)
        {
            var diff = CompareToDiff(dbName, tableName);
            var plan = new TableUpgradeOrchestrator(_dialect).Plan(diff, options);
            return TableUpgradeOrchestrator.Execute(plan, this.DatabaseId);
        }
    }
}
