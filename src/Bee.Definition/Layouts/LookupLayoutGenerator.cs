using Bee.Base;
using Bee.Definition.Forms;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Lookup layout generator.
    /// Converts a <see cref="FormSchema"/> into a <see cref="LayoutGrid"/> for the
    /// lookup picker window: one column per resolved lookup field
    /// (see <see cref="FormSchema.GetLookupFields"/>) plus a hidden
    /// <see cref="SysFields.RowId"/> column for row identification.
    /// </summary>
    internal static class LookupLayoutGenerator
    {
        /// <summary>
        /// Generates a lookup layout grid from a form schema definition.
        /// </summary>
        /// <param name="schema">The form schema definition.</param>
        /// <returns>The generated lookup grid.</returns>
        public static LayoutGrid Generate(FormSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);

            var master = schema.MasterTable;
            var grid = new LayoutGrid
            {
                TableName = schema.ProgId,
                Caption = master?.DisplayName ?? string.Empty,
                // The picker grid is selection-only; no add / edit / delete actions.
                AllowActions = GridControlAllowActions.None,
            };

            if (master?.Fields == null) return grid;

            foreach (var field in schema.GetLookupFields())
                grid.Columns!.Add(LayoutColumnFactory.ToColumn(field));

            // Hidden sys_rowid column so the grid's row-selection handler can
            // resolve the row identifier (mirrors ListLayoutGenerator).
            if (master.Fields.Contains(SysFields.RowId) &&
                !grid.Columns!.Any(c => StringUtilities.IsEquals(c.FieldName, SysFields.RowId)))
            {
                var col = LayoutColumnFactory.ToColumn(master.Fields[SysFields.RowId]);
                col.Visible = false;
                grid.Columns!.Add(col);
            }

            return grid;
        }
    }
}
