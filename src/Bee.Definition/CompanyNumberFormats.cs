using System.ComponentModel;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition
{
    /// <summary>
    /// A company-level table of <see cref="NumberKind"/> decimal-places overrides. Carries the
    /// Percent and UnitPrice/Cost display decimals plus the Quantity/Weight fallback used when no
    /// unit is bound. Amount (currency), ExchangeRate (system-fixed), and unit-bound Quantity/Weight
    /// are resolved elsewhere and are not stored here.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="MessagePackCollectionBase{T}"/> (not a keyed collection) so the table travels
    /// over the MessagePack wire as part of <c>CompanyInfo</c>; the custom MessagePack resolver only
    /// recognises this base. The keyed-lookup semantics are provided by <see cref="FindDecimals"/>.
    /// </remarks>
    [Description("Company number-format override table.")]
    [MessagePackObject]
    public class CompanyNumberFormats : MessagePackCollectionBase<NumberFormatItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompanyNumberFormats"/>.
        /// </summary>
        public CompanyNumberFormats()
        { }

        /// <summary>
        /// Finds the company override decimal places for the specified kind, or <c>null</c> when the
        /// kind is not overridden (the caller falls back to <see cref="NumberKindProfile.GetDefaultDecimals"/>).
        /// </summary>
        /// <param name="kind">The number kind.</param>
        public int? FindDecimals(NumberKind kind)
        {
            return this.FirstOrDefault(item => item.Kind == kind)?.Decimals;
        }
    }
}
