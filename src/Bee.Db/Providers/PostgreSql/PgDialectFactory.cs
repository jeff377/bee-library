using Bee.Base.Data;
using Bee.Db.Ddl;
using Bee.Db.Dml;
using Bee.Db.Schema;
using Bee.Definition.Forms;
using Bee.Definition.Storage;

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
        public IFormCommandBuilder CreateFormCommandBuilder(FormSchema formDefine, IDefineAccess defineAccess)
            => new PgFormCommandBuilder(formDefine, defineAccess);

        /// <inheritdoc />
        public string GetDefaultValueExpression(FieldDbType dbType) => PgSchemaSyntax.GetDefaultValueExpression(dbType);
    }
}
