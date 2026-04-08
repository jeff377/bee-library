using Bee.Definition.Settings;
using Bee.Definition;

namespace Bee.Repository.Abstractions.System
{
    /// <summary>
    /// Abstraction interface for database operations.
    /// </summary>
    public interface IDatabaseRepository
    {
        /// <summary>
        /// Tests the database connection and throws an exception on failure.
        /// </summary>
        /// <param name="item">The database configuration item.</param>
        void TestConnection(DatabaseItem item);

        /// <summary>
        /// Upgrades the table schema for the specified table.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        /// <remarks>Returns whether the schema was upgraded.</remarks>
        bool UpgradeTableSchema(string databaseId, string dbName, string tableName);
    }
}
