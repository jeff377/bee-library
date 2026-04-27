using Bee.Base.Data;
using Bee.Db.Ddl;
using Bee.Db.Dml;
using Bee.Db.Schema;

namespace Bee.Db.Providers
{
    /// <summary>
    /// Provider-specific factory that produces every SQL-generating or schema-reading builder the
    /// framework needs for a single <see cref="Bee.Definition.Database.DatabaseType"/>. Register an implementation
    /// with <see cref="Manager.DbDialectRegistry"/> at application startup so <see cref="Schema.TableSchemaBuilder"/>
    /// and <see cref="Schema.TableUpgradeOrchestrator"/> can route by <see cref="Bee.Definition.Database.DatabaseType"/>.
    /// </summary>
    public interface IDialectFactory
    {
        /// <summary>
        /// Creates the schema reader bound to the given database identifier.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        ITableSchemaProvider CreateTableSchemaProvider(string databaseId);

        /// <summary>
        /// Creates the CREATE TABLE builder for new tables.
        /// </summary>
        ICreateTableCommandBuilder CreateCreateTableCommandBuilder();

        /// <summary>
        /// Creates the ALTER-based change builder used by the orchestrator for in-place schema updates.
        /// </summary>
        ITableAlterCommandBuilder CreateTableAlterCommandBuilder();

        /// <summary>
        /// Creates the rebuild-fallback builder used when ALTER cannot apply all changes.
        /// </summary>
        ITableRebuildCommandBuilder CreateTableRebuildCommandBuilder();

        /// <summary>
        /// Creates a form command builder for the given form program identifier.
        /// </summary>
        /// <param name="progId">The form program identifier.</param>
        IFormCommandBuilder CreateFormCommandBuilder(string progId);

        /// <summary>
        /// Gets the provider-specific default value expression for the given logical field type,
        /// e.g. <c>getdate()</c> on SQL Server or <c>CURRENT_TIMESTAMP</c> on PostgreSQL.
        /// </summary>
        /// <param name="dbType">The logical field type.</param>
        string GetDefaultValueExpression(FieldDbType dbType);
    }
}
