using Bee.Base.Data;
using Bee.Db.Ddl;
using Bee.Db.Dml;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Forms;
using Bee.Definition.Storage;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// <see cref="IDialectFactory"/> implementation for SQL Server.
    /// </summary>
    public class SqlDialectFactory : IDialectFactory
    {
        /// <inheritdoc />
        public ITableSchemaProvider CreateTableSchemaProvider(string databaseId, IDbConnectionManager connectionManager)
            => new SqlTableSchemaProvider(databaseId, connectionManager);

        /// <inheritdoc />
        public ICreateTableCommandBuilder CreateCreateTableCommandBuilder() => new SqlCreateTableCommandBuilder();

        /// <inheritdoc />
        public ITableAlterCommandBuilder CreateTableAlterCommandBuilder() => new SqlTableAlterCommandBuilder();

        /// <inheritdoc />
        public ITableRebuildCommandBuilder CreateTableRebuildCommandBuilder() => new SqlTableRebuildCommandBuilder();

        /// <inheritdoc />
        public IFormCommandBuilder CreateFormCommandBuilder(FormSchema formDefine, IDefineAccess defineAccess)
            => new SqlFormCommandBuilder(formDefine, defineAccess);

        /// <inheritdoc />
        public string GetDefaultValueExpression(FieldDbType dbType) => SqlSchemaSyntax.GetDefaultValueExpression(dbType);
    }
}
