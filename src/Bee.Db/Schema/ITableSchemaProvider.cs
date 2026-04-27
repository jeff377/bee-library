using Bee.Definition.Database;

namespace Bee.Db.Schema
{
    /// <summary>
    /// Reads the actual schema of a database table. Each provider maps its own catalog views
    /// (SQL Server <c>sys.*</c>, PostgreSQL <c>information_schema</c> + <c>pg_catalog</c>, etc.)
    /// onto the provider-agnostic <see cref="TableSchema"/>.
    /// </summary>
    public interface ITableSchemaProvider
    {
        /// <summary>
        /// Gets the database identifier this provider is bound to.
        /// </summary>
        string DatabaseId { get; }

        /// <summary>
        /// Reads the schema of the specified table, or <c>null</c> if the table does not exist.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        TableSchema? GetTableSchema(string tableName);
    }
}
