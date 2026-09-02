using Bee.Base;
using Bee.Db.Ddl;
using Bee.Db.Schema;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Generates the SQL Server <c>MS_Description</c> extended-property calls that bring the
    /// database's stored descriptions in line with the definition during an ALTER upgrade.
    /// </summary>
    /// <remarks>
    /// SQL Server keeps descriptions as extended properties rather than in the column definition,
    /// so <c>ALTER TABLE ... ADD</c> cannot carry them: the columns this plan adds are covered here
    /// as well, always through <c>sp_addextendedproperty</c> (a column that does not exist yet
    /// cannot already carry a property, and <c>sp_updateextendedproperty</c> would fail on it).
    /// </remarks>
    public class SqlDescriptionSyncCommandBuilder : IDescriptionSyncCommandBuilder
    {
        /// <inheritdoc />
        public IReadOnlyList<string> GetStatements(TableSchemaDiff diff)
        {
            ArgumentNullException.ThrowIfNull(diff);
            var changes = DescriptionSyncChanges.Collect(diff, includeAddedColumns: true);
            string sql = SqlExtendedPropertyCommandBuilder.GetCommandText(diff.DefineTable.TableName, changes);
            return StringUtilities.IsEmpty(sql) ? Array.Empty<string>() : new[] { sql };
        }
    }
}
