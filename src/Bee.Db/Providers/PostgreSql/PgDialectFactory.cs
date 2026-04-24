using Bee.Base.Data;

namespace Bee.Db.Providers.PostgreSql
{
    /// <summary>
    /// <see cref="IDialectFactory"/> implementation for PostgreSQL. Skeleton — individual builder
    /// factory methods throw <see cref="NotImplementedException"/> until their corresponding
    /// provider classes are implemented (see PG provider plan PR 4–7).
    /// </summary>
    public class PgDialectFactory : IDialectFactory
    {
        /// <inheritdoc />
        public ITableSchemaProvider CreateTableSchemaProvider(string databaseId)
            => throw new NotImplementedException("PgTableSchemaProvider is not yet implemented (PG plan PR 7).");

        /// <inheritdoc />
        public ICreateTableCommandBuilder CreateCreateTableCommandBuilder()
            => throw new NotImplementedException("PgCreateTableCommandBuilder is not yet implemented (PG plan PR 5).");

        /// <inheritdoc />
        public ITableAlterCommandBuilder CreateTableAlterCommandBuilder()
            => throw new NotImplementedException("PgTableAlterCommandBuilder is not yet implemented (PG plan PR 6).");

        /// <inheritdoc />
        public ITableRebuildCommandBuilder CreateTableRebuildCommandBuilder()
            => throw new NotImplementedException("PgTableRebuildCommandBuilder is not yet implemented (PG plan PR 6).");

        /// <inheritdoc />
        public IFormCommandBuilder CreateFormCommandBuilder(string progId)
            => throw new NotImplementedException("PgFormCommandBuilder is not yet implemented (PG plan PR 4).");

        /// <inheritdoc />
        public string GetDefaultValueExpression(FieldDbType dbType) => PgSchemaHelper.GetDefaultValueExpression(dbType);
    }
}
