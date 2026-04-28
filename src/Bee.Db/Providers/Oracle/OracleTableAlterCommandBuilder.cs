using Bee.Db.Ddl;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;

namespace Bee.Db.Providers.Oracle
{
    /// <summary>
    /// Oracle 19c+ ALTER TABLE builder. Counterpart to
    /// <see cref="MySql.MySqlTableAlterCommandBuilder"/> and
    /// <see cref="PostgreSql.PgTableAlterCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton; the full implementation lands in a follow-up commit. Oracle uses
    /// <c>ALTER TABLE ... MODIFY (column ...)</c> for type changes and
    /// <c>ALTER TABLE ... RENAME COLUMN</c> for renames. Type changes are stricter than
    /// MySQL — for example <c>NUMBER</c> precision/scale reduction may require the column
    /// to be empty — and the rebuild fallback is invoked more often. See
    /// docs/plans/plan-oracle-support.md.
    /// </remarks>
    public class OracleTableAlterCommandBuilder : ITableAlterCommandBuilder
    {
        private const string NotImplementedMessage =
            "Oracle ALTER TABLE builder is not yet implemented. See docs/plans/plan-oracle-support.md.";

        /// <inheritdoc />
        public ChangeExecutionKind GetExecutionKind(ITableChange change) => throw new NotImplementedException(NotImplementedMessage);

        /// <inheritdoc />
        public bool IsNarrowingChange(ITableChange change) => throw new NotImplementedException(NotImplementedMessage);

        /// <inheritdoc />
        public IReadOnlyList<string> GetStatements(string tableName, ITableChange change) => throw new NotImplementedException(NotImplementedMessage);
    }
}
