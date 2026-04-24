using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition;
using Bee.Definition.Database;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Builds the SQL Server "rebuild" upgrade script (drop tmp / create tmp / copy data / drop old / rename tmp)
    /// as the fallback path when ALTER cannot apply all changes. Delegates SQL generation to
    /// <see cref="SqlCreateTableCommandBuilder"/> by reconstructing the legacy-style schema
    /// (extension fields appended, per-field upgrade actions marked).
    /// </summary>
    internal class SqlTableRebuildCommandBuilder
    {
        /// <summary>
        /// Produces the rebuild SQL script for the given diff. Extension fields (real-only) are preserved;
        /// newly added fields are marked as New so they are excluded from the INSERT ... SELECT data copy.
        /// </summary>
        /// <param name="diff">The schema diff; must not be a new-table diff (use <see cref="ICreateTableCommandBuilder"/> for that).</param>
        public static string GetCommandText(TableSchemaDiff diff)
        {
            if (diff.IsNewTable)
                throw new InvalidOperationException("Rebuild is not applicable for a new table; use CREATE TABLE instead.");

            var schema = BuildLegacyUpgradeSchema(diff);
            var inner = new SqlCreateTableCommandBuilder();
            return inner.GetCommandText(schema);
        }

        /// <summary>
        /// Reconstructs a <see cref="TableSchema"/> with legacy-style <see cref="DbUpgradeAction"/> markers
        /// so that <see cref="SqlCreateTableCommandBuilder"/> can emit the rebuild script.
        /// </summary>
        private static TableSchema BuildLegacyUpgradeSchema(TableSchemaDiff diff)
        {
            var cloned = diff.DefineTable.Clone();
            cloned.UpgradeAction = DbUpgradeAction.Upgrade;

            // Mark newly-added fields as New so INSERT ... SELECT skips them.
            var addedNames = diff.Changes.OfType<AddFieldChange>()
                .Select(c => c.Field.FieldName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (DbField field in cloned.Fields!)
            {
                if (addedNames.Contains(field.FieldName))
                    field.UpgradeAction = DbUpgradeAction.New;
            }

            // Append extension fields (present in real DB but not in the defined schema) so they are preserved.
            if (diff.RealTable != null)
            {
                foreach (var realField in diff.RealTable.Fields!)
                {
                    if (!cloned.Fields!.Contains(realField.FieldName))
                        cloned.Fields!.Add(realField.Clone());
                }
            }

            return cloned;
        }
    }
}
