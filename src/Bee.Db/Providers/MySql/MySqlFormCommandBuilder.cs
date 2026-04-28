using System.Data;
using Bee.Db.Dml;
using Bee.Definition.Filters;
using Bee.Definition.Sorting;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// MySQL 8.0+ form-related SQL command builder, generating SELECT, INSERT, UPDATE
    /// and DELETE statements. Counterpart to <see cref="Sqlite.SqliteFormCommandBuilder"/>
    /// and <see cref="PostgreSql.PgFormCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton; the full implementation lands in a follow-up commit. Differences from
    /// SQLite/PG: backtick identifier quoting and <c>LAST_INSERT_ID()</c> for retrieving
    /// auto-increment keys (within the same connection / transaction). See
    /// docs/plans/plan-mysql-support.md.
    /// </remarks>
    public class MySqlFormCommandBuilder : IFormCommandBuilder
    {
        private const string NotImplementedMessage =
            "MySQL form command builder is not yet implemented. See docs/plans/plan-mysql-support.md.";

        /// <summary>
        /// Initializes a new instance of <see cref="MySqlFormCommandBuilder"/>.
        /// </summary>
        /// <param name="progId">The form program identifier.</param>
        /// <remarks>
        /// At the stub stage <paramref name="progId"/> is intentionally not stored — the
        /// follow-up implementation will resolve <c>FormSchema</c> (see
        /// <see cref="Sqlite.SqliteFormCommandBuilder"/>) and persist it as a field.
        /// </remarks>
        public MySqlFormCommandBuilder(string progId)
        {
            _ = progId;
        }

        /// <inheritdoc />
        public DbCommandSpec BuildSelect(string tableName, string selectFields, FilterNode? filter = null, SortFieldCollection? sortFields = null)
            => throw new NotImplementedException(NotImplementedMessage);

        /// <inheritdoc />
        public DbCommandSpec BuildInsert(string tableName, DataRow row)
            => throw new NotImplementedException(NotImplementedMessage);

        /// <inheritdoc />
        public DbCommandSpec BuildUpdate(string tableName, DataRow row)
            => throw new NotImplementedException(NotImplementedMessage);

        /// <inheritdoc />
        public DbCommandSpec BuildDelete(string tableName, FilterNode filter)
            => throw new NotImplementedException(NotImplementedMessage);
    }
}
