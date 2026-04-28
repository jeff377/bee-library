using Bee.Db.Ddl;
using Bee.Db.Schema;

namespace Bee.Db.Providers.Oracle
{
    /// <summary>
    /// Oracle 19c+ rebuild-fallback builder used when in-place ALTER cannot apply all
    /// changes. Counterpart to <see cref="MySql.MySqlTableRebuildCommandBuilder"/> and
    /// <see cref="Sqlite.SqliteTableRebuildCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton; the full implementation lands in a follow-up commit. Triggered when
    /// <see cref="OracleAlterCompatibilityRules"/> classifies a change as Rebuild (e.g.
    /// cross-family type change, NUMBER precision reduction with non-empty data, IDENTITY
    /// add/remove). The rebuild path creates a tmp table, copies data, drops the original
    /// and renames the tmp — see docs/plans/plan-oracle-support.md.
    /// </remarks>
    public class OracleTableRebuildCommandBuilder : ITableRebuildCommandBuilder
    {
        private const string NotImplementedMessage =
            "Oracle rebuild builder is not yet implemented. See docs/plans/plan-oracle-support.md.";

        /// <inheritdoc />
        public string GetCommandText(TableSchemaDiff diff) => throw new NotImplementedException(NotImplementedMessage);
    }
}
