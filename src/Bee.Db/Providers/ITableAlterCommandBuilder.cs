using Bee.Db.Schema;
using Bee.Db.Schema.Changes;

namespace Bee.Db.Providers
{
    /// <summary>
    /// Provider-specific builder that translates a <see cref="TableChange"/> into executable SQL and
    /// reports whether the change can be applied via in-place ALTER.
    /// </summary>
    public interface ITableAlterCommandBuilder
    {
        /// <summary>
        /// Determines how a change can be executed by this provider.
        /// </summary>
        /// <param name="change">The structural change to classify.</param>
        ChangeExecutionKind GetExecutionKind(TableChange change);

        /// <summary>
        /// Determines whether the change narrows a column (reduces length, precision, or numeric range).
        /// Used by the orchestrator to enforce <see cref="UpgradeOptions.AllowColumnNarrowing"/>.
        /// </summary>
        /// <param name="change">The structural change to inspect.</param>
        bool IsNarrowingChange(TableChange change);

        /// <summary>
        /// Generates the SQL statements required to apply the change to the specified table.
        /// </summary>
        /// <param name="tableName">The target table name.</param>
        /// <param name="change">The change to apply; must have <see cref="ChangeExecutionKind.Alter"/> kind.</param>
        IReadOnlyList<string> GetStatements(string tableName, TableChange change);
    }
}
