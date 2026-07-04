using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.UI.Core.Permissions;

namespace Bee.UI.Avalonia.Permissions
{
    /// <summary>
    /// Applies a client capability snapshot onto a freshly generated layout by degrading its
    /// sensitive fields and grid actions in place. Safe to mutate because the layout is generated
    /// per view (via <c>FormSchema.GetFormLayout</c> / <c>GetListLayout</c>), not the cached
    /// <see cref="FormSchema"/>: capability only narrows — hides fields, marks them read-only, and
    /// removes grid actions — never widens what the layout already allows.
    /// </summary>
    internal static class LayoutCapabilityApplier
    {
        /// <summary>
        /// Degrades every master section field and detail grid of a form layout. No-op when the
        /// capability snapshot is <c>null</c> (enforcement inactive) or the schema is missing.
        /// </summary>
        public static void Apply(FormLayout? layout, FormSchema? schema, IReadOnlyDictionary<string, PermissionAction>? capabilities)
        {
            if (layout == null || schema == null || capabilities == null) { return; }

            if (layout.Sections != null)
            {
                foreach (var section in layout.Sections)
                    ApplyFields(section.Fields, schema, tableName: string.Empty, capabilities);
            }
            if (layout.Details != null)
            {
                foreach (var grid in layout.Details)
                    ApplyGrid(grid, schema, capabilities);
            }
        }

        /// <summary>
        /// Degrades a single grid: intersects its allowed actions with the form model's capability
        /// and hides / marks read-only any sensitive columns. No-op when the snapshot is <c>null</c>.
        /// </summary>
        public static void ApplyGrid(LayoutGrid? grid, FormSchema? schema, IReadOnlyDictionary<string, PermissionAction>? capabilities)
        {
            if (grid == null || schema == null || capabilities == null) { return; }

            grid.AllowActions = ElementCapabilityResolver.Default.ResolveGridActions(grid, schema, capabilities);
            ApplyFields(grid.Columns, schema, grid.TableName, capabilities);
        }

        private static void ApplyFields<T>(IEnumerable<T>? fields, FormSchema schema, string tableName, IReadOnlyDictionary<string, PermissionAction> capabilities)
            where T : LayoutFieldBase
        {
            if (fields == null) { return; }
            foreach (var field in fields)
            {
                var cap = ElementCapabilityResolver.Default.ResolveField(schema, field.FieldName, tableName, capabilities);
                // Narrow only: an already-hidden or already-read-only field stays so.
                if (!cap.Visible) { field.Visible = false; }
                if (cap.ReadOnly) { field.ReadOnly = true; }
            }
        }
    }
}
