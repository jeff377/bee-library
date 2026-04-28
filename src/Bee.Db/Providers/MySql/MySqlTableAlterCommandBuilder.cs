using Bee.Db.Ddl;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// MySQL 8.0+ ALTER TABLE builder. Counterpart to
    /// <see cref="Sqlite.SqliteTableAlterCommandBuilder"/> and
    /// <see cref="PostgreSql.PgTableAlterCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton; the full implementation lands in a follow-up commit. MySQL natively
    /// supports most schema changes (<c>ADD/DROP COLUMN</c>, <c>CHANGE COLUMN</c>,
    /// <c>MODIFY COLUMN</c>, <c>RENAME COLUMN</c> from 8.0), so the rebuild fallback
    /// is rarely required — see docs/plans/plan-mysql-support.md.
    /// </remarks>
    public class MySqlTableAlterCommandBuilder : ITableAlterCommandBuilder
    {
        private const string NotImplementedMessage =
            "MySQL ALTER TABLE builder is not yet implemented. See docs/plans/plan-mysql-support.md.";

        /// <inheritdoc />
        public ChangeExecutionKind GetExecutionKind(ITableChange change) => throw new NotImplementedException(NotImplementedMessage);

        /// <inheritdoc />
        public bool IsNarrowingChange(ITableChange change) => throw new NotImplementedException(NotImplementedMessage);

        /// <inheritdoc />
        public IReadOnlyList<string> GetStatements(string tableName, ITableChange change) => throw new NotImplementedException(NotImplementedMessage);
    }
}
