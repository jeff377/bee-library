using Bee.Base;
using Bee.Definition.Forms;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// List layout generator.
    /// Converts a <see cref="FormSchema"/> into a <see cref="LayoutGrid"/> representing
    /// the master table's list view.
    /// </summary>
    internal static class ListLayoutGenerator
    {
        /// <summary>
        /// System fields that must be added to a list grid regardless of <see cref="FormField.Visible"/>.
        /// List view does not need <see cref="SysFields.MasterRowId"/>.
        /// </summary>
        private static readonly string[] _gridIdentityFields = { SysFields.RowId };

        /// <summary>
        /// Generates a list layout grid from a form schema definition.
        /// </summary>
        /// <param name="schema">The form schema definition.</param>
        /// <returns>The generated list grid.</returns>
        public static LayoutGrid Generate(FormSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);

            var master = schema.MasterTable;
            var grid = new LayoutGrid
            {
                TableName = schema.ProgId,
                Caption = master?.DisplayName ?? string.Empty,
                AllowActions = GridControlAllowActions.All,
            };

            if (master?.Fields == null) return grid;

            // 1. Add columns by ListFields CSV (whitelist)
            string[] fieldNames = StringUtilities.Split(schema.ListFields, ",");
            foreach (var name in fieldNames.Where(master.Fields.Contains))
            {
                grid.Columns!.Add(LayoutColumnFactory.ToColumn(master.Fields[name]));
            }

            // 2. System fields required for list grid binding (whitelist), hidden in layout
            foreach (var sysName in _gridIdentityFields.Where(s =>
                master.Fields.Contains(s) &&
                !grid.Columns!.Any(c => StringUtilities.IsEquals(c.FieldName, s))))
            {
                var col = LayoutColumnFactory.ToColumn(master.Fields[sysName]);
                col.Visible = false;
                grid.Columns!.Add(col);
            }

            return grid;
        }
    }
}
