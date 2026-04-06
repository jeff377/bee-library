using Bee.Define.Database;
using Bee.Define;

namespace Bee.Db.Providers
{
    /// <summary>
    /// Defines an interface for generating CREATE TABLE SQL statements.
    /// </summary>
    public interface ICreateTableCommandBuilder
    {
        /// <summary>
        /// Gets the SQL statement for creating a table.
        /// </summary>
        /// <param name="tableSchema">The table schema definition.</param>
        string GetCommandText(TableSchema tableSchema);
    }
}
