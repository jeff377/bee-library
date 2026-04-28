using System.Data;
using Bee.Db.Dml;
using Bee.Definition.Filters;
using Bee.Definition.Sorting;

namespace Bee.Db.Providers.Oracle
{
    /// <summary>
    /// Oracle 19c+ form-related SQL command builder, generating SELECT, INSERT, UPDATE
    /// and DELETE statements. Counterpart to <see cref="MySql.MySqlFormCommandBuilder"/>
    /// and <see cref="PostgreSql.PgFormCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton; the full implementation lands in a follow-up commit. Differences from
    /// MySQL/PG: <c>:name</c> bind-variable syntax, double-quote identifier quoting, and
    /// the <c>RETURNING ... INTO</c> clause for retrieving auto-generated IDENTITY values
    /// after INSERT. See docs/plans/plan-oracle-support.md.
    /// </remarks>
    public class OracleFormCommandBuilder : IFormCommandBuilder
    {
        private const string NotImplementedMessage =
            "Oracle form command builder is not yet implemented. See docs/plans/plan-oracle-support.md.";

        /// <summary>
        /// Initializes a new instance of <see cref="OracleFormCommandBuilder"/>.
        /// </summary>
        /// <param name="progId">The form program identifier.</param>
        /// <remarks>
        /// At the stub stage <paramref name="progId"/> is intentionally not stored — the
        /// follow-up implementation will resolve <c>FormSchema</c> (see
        /// <see cref="MySql.MySqlFormCommandBuilder"/>) and persist it as a field.
        /// </remarks>
        public OracleFormCommandBuilder(string progId)
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
