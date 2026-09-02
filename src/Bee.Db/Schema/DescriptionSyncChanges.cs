using Bee.Base;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.Schema
{
    /// <summary>
    /// Collects the description entries an ALTER plan has to apply, shared by the
    /// <see cref="Bee.Db.Ddl.IDescriptionSyncCommandBuilder"/> implementations.
    /// </summary>
    internal static class DescriptionSyncChanges
    {
        /// <summary>
        /// Returns the description drift the comparer detected, optionally extended with the captions
        /// of the columns this plan is about to add.
        /// </summary>
        /// <param name="diff">The schema diff being planned.</param>
        /// <param name="includeAddedColumns">
        /// <c>true</c> for dialects that store descriptions out-of-band: a column that does not exist
        /// in the database yet produces no drift entry (the comparer only compares columns present on
        /// both sides), so without this its caption would be missing until the next upgrade round.
        /// <c>false</c> for dialects whose ADD COLUMN already carries the description inline.
        /// </param>
        public static IReadOnlyList<DescriptionChange> Collect(TableSchemaDiff diff, bool includeAddedColumns)
        {
            var list = new List<DescriptionChange>(diff.DescriptionChanges);
            if (!includeAddedColumns)
                return list;

            foreach (var add in diff.Changes.OfType<AddFieldChange>())
            {
                if (StringUtilities.IsEmpty(add.Field.Caption))
                    continue;
                list.Add(new DescriptionChange
                {
                    Level = DescriptionLevel.Column,
                    FieldName = add.Field.FieldName,
                    NewValue = add.Field.Caption,
                    // The column is being created by this very plan, so no description can exist yet.
                    IsNew = true,
                });
            }
            return list;
        }

        /// <summary>
        /// Resolves the defined field behind a column-level description entry, or <c>null</c> when the
        /// definition no longer carries it.
        /// </summary>
        /// <param name="diff">The schema diff being planned.</param>
        /// <param name="change">The column-level description entry.</param>
        public static DbField? FindDefineField(TableSchemaDiff diff, DescriptionChange change)
        {
            var fields = diff.DefineTable.Fields!;
            return fields.Contains(change.FieldName) ? fields[change.FieldName] : null;
        }
    }
}
