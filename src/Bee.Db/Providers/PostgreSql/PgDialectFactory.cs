using Bee.Base.Data;

namespace Bee.Db.Providers.PostgreSql
{
    /// <summary>
    /// <see cref="IDialectFactory"/> implementation for PostgreSQL.
    /// </summary>
    public class PgDialectFactory : IDialectFactory
    {
        /// <inheritdoc />
        public ITableSchemaProvider CreateTableSchemaProvider(string databaseId) => new PgTableSchemaProvider(databaseId);

        /// <inheritdoc />
        public ICreateTableCommandBuilder CreateCreateTableCommandBuilder() => new PgCreateTableCommandBuilder();

        /// <inheritdoc />
        public ITableAlterCommandBuilder CreateTableAlterCommandBuilder() => new PgTableAlterCommandBuilder();

        /// <inheritdoc />
        public ITableRebuildCommandBuilder CreateTableRebuildCommandBuilder() => new PgTableRebuildCommandBuilder();

        /// <inheritdoc />
        public IFormCommandBuilder CreateFormCommandBuilder(string progId) => new PgFormCommandBuilder(progId);

        /// <inheritdoc />
        public string GetDefaultValueExpression(FieldDbType dbType) => PgSchemaHelper.GetDefaultValueExpression(dbType);
    }
}
