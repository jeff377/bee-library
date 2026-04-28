using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.Providers.Oracle
{
    /// <summary>
    /// Oracle 19c+ schema provider. Reads <c>ALL_TAB_COLUMNS</c>, <c>ALL_INDEXES</c>,
    /// <c>ALL_CONSTRAINTS</c> and <c>ALL_COL_COMMENTS</c> and maps the result onto
    /// <see cref="TableSchema"/>. Counterpart to <see cref="MySql.MySqlTableSchemaProvider"/>
    /// and <see cref="PostgreSql.PgTableSchemaProvider"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton; the full implementation lands in a follow-up commit. The PostgreSQL
    /// counterpart is the closer reference because Oracle's <c>ALL_*</c> dictionary
    /// views play the same role as <c>INFORMATION_SCHEMA</c> — see
    /// docs/plans/plan-oracle-support.md.
    /// </remarks>
    public class OracleTableSchemaProvider : ITableSchemaProvider
    {
        private const string NotImplementedMessage =
            "Oracle schema provider is not yet implemented. See docs/plans/plan-oracle-support.md.";

        /// <summary>
        /// Initializes a new instance of <see cref="OracleTableSchemaProvider"/>.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        public OracleTableSchemaProvider(string databaseId)
        {
            DatabaseId = databaseId;
        }

        /// <inheritdoc />
        public string DatabaseId { get; }

        /// <inheritdoc />
        public TableSchema? GetTableSchema(string tableName) => throw new NotImplementedException(NotImplementedMessage);
    }
}
