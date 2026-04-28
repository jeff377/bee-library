using Bee.Db.Ddl;
using Bee.Definition.Database;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// MySQL 8.0+ CREATE TABLE builder. Counterpart to
    /// <see cref="Sqlite.SqliteCreateTableCommandBuilder"/> and
    /// <see cref="PostgreSql.PgCreateTableCommandBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Skeleton; the full implementation lands in a follow-up commit. Dialect decisions
    /// (utf8mb4 / COLLATE=utf8mb4_0900_ai_ci day-1 CI baseline, ENGINE=InnoDB,
    /// AUTO_INCREMENT, BIGINT/DATETIME(6)/CHAR(36)) are documented in
    /// docs/plans/plan-mysql-support.md.
    /// </remarks>
    public class MySqlCreateTableCommandBuilder : ICreateTableCommandBuilder
    {
        private const string NotImplementedMessage =
            "MySQL CREATE TABLE builder is not yet implemented. See docs/plans/plan-mysql-support.md.";

        /// <inheritdoc />
        public string GetCommandText(TableSchema tableSchema) => throw new NotImplementedException(NotImplementedMessage);
    }
}
