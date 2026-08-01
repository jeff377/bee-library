using Bee.Definition.Forms;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Takes the display text of a hand-authored <see cref="FormLayout"/> from an already-localized
    /// <see cref="FormSchema"/>, so a layout definition file describes structure only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generated layout inherits localized text for free — the framework localizes the schema
    /// first and the generator copies each <see cref="FormField.Caption"/> into the layout. A layout
    /// read from a definition file does not: its captions are whatever the author typed, frozen in
    /// one language. This applier closes that gap, so both paths behave alike and the split of
    /// responsibilities stays clean: language resources own the words, layout files own the
    /// arrangement.
    /// </para>
    /// <para>
    /// Fields the schema does not know about keep the text the layout file gives them. A layout file
    /// can legitimately lag behind its schema, and silently blanking those captions would turn a
    /// stale file into an unreadable form.
    /// </para>
    /// <para>
    /// The layout is mutated in place, so callers must not pass a shared cache instance — clone
    /// first (<see cref="FormLayout.Clone"/>). This mirrors <c>FormSchemaLocalizer</c>, which makes
    /// the same demand for the same reason.
    /// </para>
    /// </remarks>
    public static class FormLayoutCaptionApplier
    {
        /// <summary>
        /// Applies the schema's display text to <paramref name="layout"/>.
        /// </summary>
        /// <param name="layout">The layout to mutate. Must not be a shared cache instance — clone first.</param>
        /// <param name="schema">The localized schema supplying the text.</param>
        public static void Apply(FormLayout layout, FormSchema schema)
        {
            ArgumentNullException.ThrowIfNull(layout);
            ArgumentNullException.ThrowIfNull(schema);

            layout.Caption = schema.DisplayName;

            var master = schema.MasterTable;
            if (layout.Sections != null && master != null)
            {
                foreach (var section in layout.Sections)
                {
                    section.Caption = master.DisplayName;
                    ApplyFieldCaptions(section, master);
                }
            }

            if (layout.Details == null || schema.Tables == null)
                return;

            foreach (var grid in layout.Details)
            {
                if (string.IsNullOrWhiteSpace(grid.TableName) || !schema.Tables.Contains(grid.TableName))
                    continue;
                var table = schema.Tables[grid.TableName];
                grid.Caption = table.DisplayName;
                ApplyColumnCaptions(grid, table);
            }
        }

        private static void ApplyFieldCaptions(LayoutSection section, FormTable table)
        {
            if (section.Fields == null || table.Fields == null)
                return;
            foreach (var field in section.Fields)
            {
                if (!string.IsNullOrWhiteSpace(field.FieldName) && table.Fields.Contains(field.FieldName))
                    field.Caption = table.Fields[field.FieldName].Caption;
            }
        }

        private static void ApplyColumnCaptions(LayoutGrid grid, FormTable table)
        {
            if (grid.Columns == null || table.Fields == null)
                return;
            foreach (var column in grid.Columns)
            {
                if (!string.IsNullOrWhiteSpace(column.FieldName) && table.Fields.Contains(column.FieldName))
                    column.Caption = table.Fields[column.FieldName].Caption;
            }
        }
    }
}
