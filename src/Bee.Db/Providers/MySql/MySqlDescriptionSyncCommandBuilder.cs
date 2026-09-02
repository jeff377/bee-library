using Bee.Base;
using Bee.Db.Ddl;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// Generates the MySQL statements that bring the database's stored comments in line with the
    /// definition during an ALTER upgrade.
    /// </summary>
    /// <remarks>
    /// MySQL carries a column's comment inside the column definition, so
    /// <see cref="MySqlSchemaSyntax.GetColumnDefinition"/> already applies it on every
    /// <c>ADD COLUMN</c> / <c>MODIFY COLUMN</c> this plan emits. Only the drift those statements do
    /// not cover is handled here — a caption that changed with no structural change alongside it,
    /// and the table-level comment, which has no column definition to ride on. Columns that already
    /// have an <see cref="AlterFieldChange"/> in the same plan are skipped: re-issuing
    /// <c>MODIFY COLUMN</c> for them would be a second full table rebuild for no effect.
    /// </remarks>
    public class MySqlDescriptionSyncCommandBuilder : IDescriptionSyncCommandBuilder
    {
        /// <inheritdoc />
        public IReadOnlyList<string> GetStatements(TableSchemaDiff diff)
        {
            ArgumentNullException.ThrowIfNull(diff);
            string tableName = diff.DefineTable.TableName;
            string quotedTable = MySqlSchemaSyntax.QuoteName(tableName);
            var alteredColumns = diff.Changes.OfType<AlterFieldChange>()
                .Select(c => c.NewField.FieldName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var statements = new List<string>();
            foreach (var change in DescriptionSyncChanges.Collect(diff, includeAddedColumns: false))
            {
                if (StringUtilities.IsEmpty(change.NewValue))
                    continue;

                if (change.Level == DescriptionLevel.Table)
                {
                    statements.Add($"ALTER TABLE {quotedTable} COMMENT = '{MySqlSchemaSyntax.EscapeSqlString(change.NewValue)}';");
                    continue;
                }

                if (alteredColumns.Contains(change.FieldName))
                    continue;

                var field = DescriptionSyncChanges.FindDefineField(diff, change);
                if (field == null)
                    continue;
                statements.Add($"ALTER TABLE {quotedTable} MODIFY COLUMN {MySqlSchemaSyntax.GetModifyColumnDefinition(field)};");
            }
            return statements;
        }
    }
}
