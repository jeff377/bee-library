using Bee.Base.Data;
using Bee.Db.Ddl;
using Bee.Db.Dml;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Forms;
using Bee.Definition.Storage;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// <see cref="IDialectFactory"/> implementation for MySQL 8.0+.
    /// </summary>
    /// <remarks>
    /// Skeleton: factory wiring is in place; the create-builder methods produce stub
    /// builders that throw <see cref="NotImplementedException"/>. Full implementation of
    /// the CREATE / ALTER / Rebuild / Form / SchemaProvider builders lands in follow-up
    /// commits — see docs/plans/plan-mysql-support.md.
    /// </remarks>
    public class MySqlDialectFactory : IDialectFactory
    {
        /// <inheritdoc />
        public ITableSchemaProvider CreateTableSchemaProvider(string databaseId, IDbConnectionManager connectionManager)
            => new MySqlTableSchemaProvider(databaseId, connectionManager);

        /// <inheritdoc />
        public ICreateTableCommandBuilder CreateCreateTableCommandBuilder() => new MySqlCreateTableCommandBuilder();

        /// <inheritdoc />
        public ITableAlterCommandBuilder CreateTableAlterCommandBuilder() => new MySqlTableAlterCommandBuilder();

        /// <inheritdoc />
        public ITableRebuildCommandBuilder CreateTableRebuildCommandBuilder() => new MySqlTableRebuildCommandBuilder();

        /// <inheritdoc />
        public IFormCommandBuilder CreateFormCommandBuilder(FormSchema formDefine, IDefineAccess defineAccess)
            => new MySqlFormCommandBuilder(formDefine, defineAccess);

        /// <inheritdoc />
        public string GetDefaultValueExpression(FieldDbType dbType) =>
            MySqlSchemaSyntax.GetDefaultValueExpression(dbType);
    }
}
