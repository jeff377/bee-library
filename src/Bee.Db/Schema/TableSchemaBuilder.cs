using Bee.Definition.Database;
using Bee.Base;
using Bee.Definition;

using Bee.Db.DbAccess;
using DbAccessObject = Bee.Db.DbAccess.DbAccess;
using Bee.Db.Providers;
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
        /// Compares the actual table schema with the defined schema and returns the comparison result.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema Compare(string dbName, string tableName)
        {
            // Actual table schema from the database
            var provider = new SqlTableSchemaProvider(this.DatabaseId);
            var realTable = provider.GetTableSchema(tableName);
            // Defined table schema from the form definitions
            var defineTable = BackendInfo.DefineAccess.GetTableSchema(dbName, tableName);
            // Compare and return the resulting table schema
            var comparer = new TableSchemaComparer(defineTable, realTable);
            return comparer.Compare();
        }

        /// <summary>
        /// Compares the table schema and returns the SQL command text required for the upgrade.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public string GetCommandText(string dbName, string tableName)
        {
            // Compare table schemas and retrieve the schema that requires upgrading
            var dbTable = this.Compare(dbName, tableName);
            if (dbTable.UpgradeAction != DbUpgradeAction.None)
            {
                var builder = new SqlCreateTableCommandBuilder();
                return builder.GetCommandText(dbTable);
            }
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
                var dbAccess = new DbAccessObject(DatabaseId);
                dbAccess.Execute(command);
                return true;
            }
            return false;
        }
    }
}
