using Bee.Base;
using Bee.Db.Ddl;
using Bee.Db.Schema;

namespace Bee.Db.Providers.PostgreSql
{
    /// <summary>
    /// Generates the PostgreSQL <c>COMMENT ON TABLE</c> / <c>COMMENT ON COLUMN</c> statements that
    /// bring the database's stored descriptions in line with the definition during an ALTER upgrade.
    /// </summary>
    /// <remarks>
    /// PostgreSQL keeps descriptions in <c>pg_description</c> rather than in the column definition,
    /// so <c>ALTER TABLE ... ADD COLUMN</c> cannot carry them: the columns this plan adds are covered
    /// here as well. <c>COMMENT ON</c> is an upsert, so <see cref="DescriptionChange.IsNew"/> is not
    /// consulted.
    /// </remarks>
    public class PgDescriptionSyncCommandBuilder : IDescriptionSyncCommandBuilder
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
                    ? PgSchemaSyntax.GetTableCommentStatement(tableName, change.NewValue)
                    : PgSchemaSyntax.GetColumnCommentStatement(tableName, change.FieldName, change.NewValue));
            }
            return statements;
        }
    }
}
