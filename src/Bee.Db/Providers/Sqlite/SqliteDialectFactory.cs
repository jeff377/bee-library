using Bee.Base.Data;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// <see cref="IDialectFactory"/> implementation for SQLite.
    /// Skeleton: factory wiring is in place but the create-builder methods will land in the
    /// follow-up PRs (S2 form CRUD, S3 CREATE TABLE, S4 ALTER / REBUILD, S5 schema reader).
    /// </summary>
    public class SqliteDialectFactory : IDialectFactory
    {
        /// <inheritdoc />
        public ITableSchemaProvider CreateTableSchemaProvider(string databaseId) =>
            throw new NotImplementedException("SqliteTableSchemaProvider lands in PR S5.");

        /// <inheritdoc />
        public ICreateTableCommandBuilder CreateCreateTableCommandBuilder() => new SqliteCreateTableCommandBuilder();

        /// <inheritdoc />
        public ITableAlterCommandBuilder CreateTableAlterCommandBuilder() => new SqliteTableAlterCommandBuilder();

        /// <inheritdoc />
        public ITableRebuildCommandBuilder CreateTableRebuildCommandBuilder() => new SqliteTableRebuildCommandBuilder();

        /// <inheritdoc />
        public IFormCommandBuilder CreateFormCommandBuilder(string progId) => new SqliteFormCommandBuilder(progId);

        /// <inheritdoc />
        public string GetDefaultValueExpression(FieldDbType dbType) =>
            SqliteSchemaHelper.GetDefaultValueExpression(dbType);
    }
}
