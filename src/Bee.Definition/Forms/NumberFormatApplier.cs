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
            return EnumerateFields(schema).Any(field => field.NumberKind != NumberKind.None);
        }

        /// <summary>Enumerates every field across all tables of the schema (skipping empty tables).</summary>
        private static IEnumerable<FormField> EnumerateFields(FormSchema schema)
            => (schema.Tables ?? Enumerable.Empty<FormTable>())
                .Where(table => table.Fields != null)
                .SelectMany(table => table.Fields!);

        /// <summary>
        /// Bakes <see cref="FormField.NumberFormat"/> onto every field with a semantic
        /// <see cref="NumberKind"/> that has no explicit format, using
        /// <see cref="NumberFormatResolver.ResolveFormat(NumberKind, CompanyInfo?)"/>. An explicit
        /// <see cref="FormField.NumberFormat"/> always wins and is left untouched;
        /// <see cref="NumberKind.None"/> fields are skipped.
        /// </summary>
        /// <remarks>
        /// <see cref="DecimalsSource.Currency"/> amounts are deliberately <b>not</b> baked: their
        /// decimals depend on the amount's runtime currency and are resolved by the UI (see
        /// plan-numeric-multicurrency.md §2.1 / §3.2). Instead, an amount field with no explicit
        /// <see cref="FormField.CurrencyField"/> inherits the master document currency field
        /// (<see cref="FormSchema.CurrencyField"/>) so every amount carries a concrete currency
        /// reference for the UI to resolve against. Likewise, <see cref="DecimalsSource.Unit"/>
        /// quantities/weights that bind a <see cref="FormField.UnitField"/> are not baked (runtime by
        /// unit); unbound ones fall back to the company decimals and are baked. Company and system-fixed
        /// kinds are baked here.
        /// </remarks>
        /// <param name="schema">The schema to bake (mutated in place — pass a clone).</param>
        /// <param name="company">The current company, or <c>null</c> to use framework defaults.</param>
        public static void Bake(FormSchema schema, CompanyInfo? company)
        {
            ArgumentNullException.ThrowIfNull(schema);

            foreach (var field in EnumerateFields(schema))
            {
                if (field.NumberKind == NumberKind.None) { continue; }
                // Explicit author-supplied format wins and is preserved.
                if (StringUtilities.IsNotEmpty(field.NumberFormat)) { continue; }

                var source = NumberKindProfile.GetDecimalsSource(field.NumberKind);
                if (source == DecimalsSource.Currency)
                {
                    // Runtime-resolved by currency; do not bake a format. Stamp the effective
                    // currency-reference field so the UI knows which field holds this amount's currency.
                    if (StringUtilities.IsEmpty(field.CurrencyField) && StringUtilities.IsNotEmpty(schema.CurrencyField))
                        field.CurrencyField = schema.CurrencyField;
                    continue;
                }

                // Quantities/weights bound to a unit field are runtime-resolved by unit — do not bake.
                // Without a bound unit, they fall back to the company decimals and are baked here.
                if (source == DecimalsSource.Unit && StringUtilities.IsNotEmpty(field.UnitField))
                    continue;

                field.NumberFormat = NumberFormatResolver.ResolveFormat(field.NumberKind, company);
            }
        }
    }
}
