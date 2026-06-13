using Bee.Base;
using Bee.Definition.Forms;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Form layout generator.
    /// Converts a <see cref="FormSchema"/> into a single-record-mode <see cref="FormLayout"/>
    /// (master sections + detail grids).
    /// </summary>
    internal static class FormLayoutGenerator
    {
        /// <summary>
        /// System fields that must be added to detail grids regardless of <see cref="FormField.Visible"/>.
        /// Grid controls require these for row binding and master association.
        /// </summary>
        private static readonly string[] _gridIdentityFields =
            { SysFields.RowId, SysFields.MasterRowId };

        /// <summary>
        /// Generates a form layout from a form schema definition.
        /// </summary>
        /// <param name="schema">The form schema definition.</param>
        /// <param name="layoutId">The layout ID to assign.</param>
        /// <returns>The generated form layout.</returns>
        public static FormLayout Generate(FormSchema schema, string layoutId)
        {
            ArgumentNullException.ThrowIfNull(schema);

            var layout = new FormLayout
            {
                LayoutId = layoutId,
                ProgId = schema.ProgId,
                Caption = schema.DisplayName,
                ColumnCount = 2,
            };

            AddSections(schema, layout);
            AddDetails(schema, layout);

            return layout;
        }

        /// <summary>
        /// Adds the master area as a single default section "Main".
        /// </summary>
        private static void AddSections(FormSchema schema, FormLayout layout)
        {
            var master = schema.MasterTable;
            if (master?.Fields == null) return;

            var section = new LayoutSection
            {
                Name = "Main",
                Caption = master.DisplayName,
            };

            var covered = CollectCoveredDisplayFields(master);
            foreach (var field in master.Fields.Where(f => ShouldEmit(f, covered)))
                section.Fields!.Add(LayoutColumnFactory.ToField(field));

            if (section.Fields!.Count > 0)
                layout.Sections!.Add(section);
        }

        /// <summary>
        /// Adds one detail grid for each non-master table.
        /// </summary>
        private static void AddDetails(FormSchema schema, FormLayout layout)
        {
            if (schema.Tables == null) return;

            foreach (var table in schema.Tables.Where(t => t != schema.MasterTable))
            {
                if (table.Fields == null) continue;

                var grid = new LayoutGrid
                {
                    TableName = table.TableName,
                    Caption = table.DisplayName,
                    AllowActions = GridControlAllowActions.All,
                };

                // 1. Emitted fields (relation lookup editors + visible plain fields)
                var covered = CollectCoveredDisplayFields(table);
                foreach (var field in table.Fields.Where(f => ShouldEmit(f, covered)))
                    grid.Columns!.Add(LayoutColumnFactory.ToColumn(field));

                // 2. System fields required for grid binding (whitelist), hidden in layout
                foreach (var sysName in _gridIdentityFields.Where(s =>
                    table.Fields.Contains(s) &&
                    !grid.Columns!.Any(c => StringUtilities.IsEquals(c.FieldName, s))))
                {
                    var col = LayoutColumnFactory.ToColumn(table.Fields[sysName]);
                    col.Visible = false;
                    grid.Columns!.Add(col);
                }

                if (grid.Columns!.Count > 0)
                    layout.Details!.Add(grid);
            }
        }

        /// <summary>
        /// Collects the fields expressed through a relation field's composite lookup
        /// display (its effective <c>DisplayFields</c>); they are not generated as
        /// separate layout fields.
        /// </summary>
        private static HashSet<string> CollectCoveredDisplayFields(FormTable table)
        {
            var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in table.Fields!.Where(f => StringUtilities.IsNotEmpty(f.RelationProgId)))
            {
                foreach (var name in field.GetDisplayFields())
                    covered.Add(name);
            }
            return covered;
        }

        /// <summary>
        /// Whether the field gets its own layout field. A relation field always renders
        /// — its lookup editor carries the display fields, and the intuitive authoring
        /// marks the raw row-id value invisible (<c>Visible=false</c>) because the raw
        /// Guid is never shown as data. Plain fields follow <see cref="FormField.Visible"/>
        /// and skip the ones a lookup display already covers; uncovered relation display
        /// fields (e.g. extra <c>ref_*</c> projections) still render on their own.
        /// </summary>
        private static bool ShouldEmit(FormField field, HashSet<string> covered)
        {
            if (StringUtilities.IsNotEmpty(field.RelationProgId)) return true;
            if (!field.Visible) return false;
            return !covered.Contains(field.FieldName);
        }
    }
}
