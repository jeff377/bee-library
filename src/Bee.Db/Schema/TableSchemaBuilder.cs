using System.Text;
using Bee.Base;
using Bee.Db.Manager;
using Bee.Db.Providers;
using Bee.Definition.Database;
using Bee.Definition.Storage;

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
        private readonly DatabaseType _databaseType;
        private readonly IDefineAccess _defineAccess;
        private readonly IDbConnectionManager _connectionManager;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="TableSchemaBuilder"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        /// <param name="defineAccess">The define access service used to fetch the defined table schema.</param>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public TableSchemaBuilder(string databaseId, IDefineAccess defineAccess, IDbConnectionManager connectionManager)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            DatabaseId = databaseId;
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            var connInfo = connectionManager.GetConnectionInfo(databaseId);
            _databaseType = connInfo.DatabaseType;
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
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        private TableSchemaComparer CreateComparer(string categoryId, string tableName)
        {
            // Actual table schema from the database
            var provider = _dialect.CreateTableSchemaProvider(this.DatabaseId, _connectionManager);
            var realTable = provider.GetTableSchema(tableName);
            // Defined table schema from the form definitions
            var defineTable = _defineAccess.GetTableSchema(categoryId, tableName);
            return new TableSchemaComparer(defineTable, realTable, _databaseType);
        }

        /// <summary>
        /// Compares the actual table schema with the defined schema and returns the legacy comparison result
        /// (retained for existing callers; new code should use <see cref="CompareToDiff"/>).
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema Compare(string categoryId, string tableName)
        {
            return CreateComparer(categoryId, tableName).Compare();
        }

        /// <summary>
        /// Produces a structured <see cref="TableSchemaDiff"/> for the specified table.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchemaDiff CompareToDiff(string categoryId, string tableName)
        {
            return CreateComparer(categoryId, tableName).CompareToDiff();
        }

        /// <summary>
        /// Plans the upgrade for the specified table and returns the combined SQL script (all stages concatenated).
        /// Returns an empty string when no changes are required.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="options">Upgrade options; null uses <see cref="UpgradeOptions.Default"/>.</param>
        public string GetCommandText(string categoryId, string tableName, UpgradeOptions? options = null)
        {
            var diff = CompareToDiff(categoryId, tableName);
            var plan = new TableUpgradeOrchestrator(_dialect, _connectionManager).Plan(diff, options);
            if (plan.IsEmpty) return string.Empty;

            var sb = new StringBuilder();
            foreach (var sql in plan.AllStatements)
            {
                if (StringUtilities.IsEmpty(sql)) continue;
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(sql);
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Compares the table schema and executes the upgrade if differences are found.
        /// Each stage runs in its own transaction.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="options">Upgrade options; null uses <see cref="UpgradeOptions.Default"/>.</param>
        /// <remarks>Returns <c>true</c> if an upgrade was performed; otherwise <c>false</c>.</remarks>
        public bool Execute(string categoryId, string tableName, UpgradeOptions? options = null)
        {
            var diff = CompareToDiff(categoryId, tableName);
            var orchestrator = new TableUpgradeOrchestrator(_dialect, _connectionManager);
            var plan = orchestrator.Plan(diff, options);
            return orchestrator.Execute(plan, this.DatabaseId);
        }
    }
}
