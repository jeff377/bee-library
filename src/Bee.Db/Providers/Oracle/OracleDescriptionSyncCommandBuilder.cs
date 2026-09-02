using Bee.Base;
using Bee.Db.Ddl;
using Bee.Db.Schema;

namespace Bee.Db.Providers.Oracle
{
    /// <summary>
    /// Generates the Oracle <c>COMMENT ON TABLE</c> / <c>COMMENT ON COLUMN</c> statements that
    /// bring the database's stored captions in line with the definition during an ALTER upgrade.
    /// </summary>
    /// <remarks>
    /// Oracle stores captions out-of-band in <c>USER_TAB_COMMENTS</c> / <c>USER_COL_COMMENTS</c>,
    /// so <c>ALTER TABLE ... ADD</c> cannot carry them: the columns this plan adds are covered here
    /// as well. <c>COMMENT ON</c> is an upsert, so <see cref="DescriptionChange.IsNew"/> is not
    /// consulted. Each statement is dispatched on its own — Oracle.ManagedDataAccess accepts only
    /// one statement per command (ORA-03405), so no trailing semicolons are emitted.
    /// </remarks>
    public class OracleDescriptionSyncCommandBuilder : IDescriptionSyncCommandBuilder
    {
        /// <inheritdoc />
        public IReadOnlyList<string> GetStatements(TableSchemaDiff diff)
        {
            ArgumentNullException.ThrowIfNull(diff);
            string tableName = diff.DefineTable.TableName;
            var statements = new List<string>();
            foreach (var change in DescriptionSyncChanges.Collect(diff, includeAddedColumns: true))
            {
                if (StringUtilities.IsEmpty(change.NewValue))
                    continue;
                statements.Add(change.Level == DescriptionLevel.Table
                    ? OracleSchemaSyntax.GetTableCommentStatement(tableName, change.NewValue)
                    : OracleSchemaSyntax.GetColumnCommentStatement(tableName, change.FieldName, change.NewValue));
            }
            return statements;
        }
    }
}
