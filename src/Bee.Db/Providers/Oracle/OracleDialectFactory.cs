using Bee.Base.Data;
using Bee.Db.Ddl;
using Bee.Db.Dml;
using Bee.Db.Schema;

namespace Bee.Db.Providers.Oracle
{
    /// <summary>
    /// <see cref="IDialectFactory"/> implementation for Oracle 19c+.
    /// </summary>
    /// <remarks>
    /// Skeleton: factory wiring is in place; the create-builder methods produce stub
    /// builders that throw <see cref="NotImplementedException"/>. Full implementation of
    /// the CREATE / ALTER / Rebuild / Form / SchemaProvider builders lands in follow-up
    /// commits — see docs/plans/plan-oracle-support.md.
    /// </remarks>
    public class OracleDialectFactory : IDialectFactory
    {
        /// <inheritdoc />
        public ITableSchemaProvider CreateTableSchemaProvider(string databaseId) => new OracleTableSchemaProvider(databaseId);

        /// <inheritdoc />
        public ICreateTableCommandBuilder CreateCreateTableCommandBuilder() => new OracleCreateTableCommandBuilder();

        /// <inheritdoc />
        public ITableAlterCommandBuilder CreateTableAlterCommandBuilder() => new OracleTableAlterCommandBuilder();

        /// <inheritdoc />
        public ITableRebuildCommandBuilder CreateTableRebuildCommandBuilder() => new OracleTableRebuildCommandBuilder();

        /// <inheritdoc />
        public IFormCommandBuilder CreateFormCommandBuilder(string progId) => new OracleFormCommandBuilder(progId);

        /// <inheritdoc />
        public string GetDefaultValueExpression(FieldDbType dbType) =>
            OracleSchemaHelper.GetDefaultValueExpression(dbType);
    }
}
