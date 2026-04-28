using Bee.Db.Ddl;
using Bee.Db.Schema;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// MySQL 8.0+ rebuild-fallback builder used when in-place ALTER cannot apply all
    /// changes. Counterpart to <see cref="Sqlite.SqliteTableRebuildCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton; the full implementation lands in a follow-up commit. Because MySQL
    /// 8.0 natively supports almost every change kind via <c>ALTER TABLE</c>, the
    /// rebuild path is reserved for edge cases (e.g. some primary-key restructures).
    /// </remarks>
    public class MySqlTableRebuildCommandBuilder : ITableRebuildCommandBuilder
    {
        private const string NotImplementedMessage =
            "MySQL rebuild builder is not yet implemented. See docs/plans/plan-mysql-support.md.";

        /// <inheritdoc />
        public string GetCommandText(TableSchemaDiff diff) => throw new NotImplementedException(NotImplementedMessage);
    }
}
