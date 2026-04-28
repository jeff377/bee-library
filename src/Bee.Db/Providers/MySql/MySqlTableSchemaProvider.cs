using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// MySQL 8.0+ schema provider. Reads <c>INFORMATION_SCHEMA</c>
    /// (<c>COLUMNS</c> / <c>KEY_COLUMN_USAGE</c> / <c>STATISTICS</c>) and maps the result
    /// onto <see cref="TableSchema"/>. Counterpart to
    /// <see cref="Sqlite.SqliteTableSchemaProvider"/> and
    /// <see cref="PostgreSql.PgTableSchemaProvider"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton; the full implementation lands in a follow-up commit. The PostgreSQL
    /// counterpart is the closer reference because it also queries
    /// <c>INFORMATION_SCHEMA</c> — see docs/plans/plan-mysql-support.md.
    /// </remarks>
    public class MySqlTableSchemaProvider : ITableSchemaProvider
    {
        private const string NotImplementedMessage =
            "MySQL schema provider is not yet implemented. See docs/plans/plan-mysql-support.md.";

        /// <summary>
        /// Initializes a new instance of <see cref="MySqlTableSchemaProvider"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public MySqlTableSchemaProvider(string databaseId)
        {
            DatabaseId = databaseId;
        }

        /// <inheritdoc />
        public string DatabaseId { get; }

        /// <inheritdoc />
        public TableSchema? GetTableSchema(string tableName) => throw new NotImplementedException(NotImplementedMessage);
    }
}
