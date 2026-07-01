using Bee.Base;
using Bee.Definition.Identity;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// Bakes company-aware display formats onto a <see cref="FormSchema"/>'s numeric fields at
    /// delivery time. Mutates the given schema in place, so callers must pass a clone — never the
    /// shared cached schema (see the immutability note on <c>SystemBusinessObject.LoadAndLocalizeSchema</c>).
    /// </summary>
    public static class NumberFormatApplier
    {
        /// <summary>
        /// Returns whether the schema has any field carrying a semantic <see cref="NumberKind"/>
        /// (i.e. any field that <see cref="Bake"/> could format). Used to skip cloning when there is
        /// nothing to bake.
        /// </summary>
        /// <param name="schema">The schema to inspect (not mutated).</param>
        public static bool HasNumericField(FormSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);
            if (schema.Tables == null) { return false; }

            foreach (var table in schema.Tables)
            {
                if (table.Fields == null) { continue; }
                foreach (var field in table.Fields)
                {
                    if (field.NumberKind != NumberKind.None) { return true; }
                }
            }
            return false;
        }

        /// <summary>
        /// Bakes <see cref="FormField.NumberFormat"/> onto every field with a semantic
        /// <see cref="NumberKind"/> that has no explicit format, using
        /// <see cref="NumberFormatResolver.ResolveFormat"/>. An explicit <see cref="FormField.NumberFormat"/>
        /// always wins and is left untouched; <see cref="NumberKind.None"/> fields are skipped.
        /// </summary>
        /// <param name="schema">The schema to bake (mutated in place — pass a clone).</param>
        /// <param name="company">The current company, or <c>null</c> to use framework defaults.</param>
        public static void Bake(FormSchema schema, CompanyInfo? company)
        {
            ArgumentNullException.ThrowIfNull(schema);
            if (schema.Tables == null) { return; }

            foreach (var table in schema.Tables)
            {
                if (table.Fields == null) { continue; }
                foreach (var field in table.Fields)
                {
                    if (field.NumberKind == NumberKind.None) { continue; }
                    // Explicit author-supplied format wins and is preserved.
                    if (StringUtilities.IsNotEmpty(field.NumberFormat)) { continue; }
                    field.NumberFormat = NumberFormatResolver.ResolveFormat(field.NumberKind, company);
                }
            }
        }
    }
}
