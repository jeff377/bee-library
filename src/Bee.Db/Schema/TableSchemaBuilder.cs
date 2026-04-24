using Bee.Definition.Database;
using Bee.Base;
using Bee.Definition;
using Bee.Db.Providers.SqlServer;

namespace Bee.Db.Schema
{
    /// <summary>
    /// Compares and builds table schema upgrade commands.
    /// </summary>
    public class TableSchemaBuilder
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="TableSchemaBuilder"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public TableSchemaBuilder(string databaseId)
        {
            DatabaseId = databaseId;
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
            var provider = new SqlTableSchemaProvider(this.DatabaseId);
            var realTable = provider.GetTableSchema(tableName);
            // Defined table schema from the form definitions
            var defineTable = BackendInfo.DefineAccess.GetTableSchema(dbName, tableName);
            return new TableSchemaComparer(defineTable, realTable);
        }

        /// <summary>
        /// Compares the actual table schema with the defined schema and returns the comparison result.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema Compare(string dbName, string tableName)
        {
            return CreateComparer(dbName, tableName).Compare();
        }

        /// <summary>
        /// Compares the table schema and returns the SQL command text required for the upgrade.
        /// Schema changes go through <see cref="SqlCreateTableCommandBuilder"/>; when only description
        /// drift exists, a metadata-only script is produced via <see cref="SqlExtendedPropertyCommandBuilder"/>.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public string GetCommandText(string dbName, string tableName)
        {
            var comparer = CreateComparer(dbName, tableName);
            var dbTable = comparer.Compare();
            // Schema DDL path: CREATE or rebuild (already includes extended property writes)
            if (dbTable.UpgradeAction != DbUpgradeAction.None)
            {
                var schemaBuilder = new SqlCreateTableCommandBuilder();
                return schemaBuilder.GetCommandText(dbTable);
            }
            // Metadata-only path: description drift without schema change
            if (comparer.DescriptionChanges.Count > 0)
                return SqlExtendedPropertyCommandBuilder.GetCommandText(dbTable.TableName, comparer.DescriptionChanges);
            return string.Empty;
        }

        /// <summary>
        /// Compares the table schema and executes the upgrade if differences are found.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        /// <remarks>Returns <c>true</c> if an upgrade was performed; otherwise <c>false</c>.</remarks>
        public bool Execute(string dbName, string tableName)
        {
            string sql = this.GetCommandText(dbName, tableName);
            if (StrFunc.IsNotEmpty(sql))
            {
                var command = new DbCommandSpec(DbCommandKind.NonQuery, sql);
                var dbAccess = new DbAccess(DatabaseId);
                dbAccess.Execute(command);
                return true;
            }
            return false;
        }
    }
}
