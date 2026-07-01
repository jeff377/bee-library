using System.ComponentModel;
using Bee.Definition.Collections;
using Bee.Definition.Settings;
using MessagePack;

namespace Bee.Definition
{
    /// <summary>
    /// A company-level table of per-currency cash-rounding overrides (SAP T001R-style). Empty means
    /// the company applies no extra cash rounding — the final amount stays at the currency's natural
    /// minor unit. Carried by <c>CompanyInfo</c> over the MessagePack wire.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="MessagePackCollectionBase{T}"/> so the table serializes cleanly as part of
    /// <c>CompanyInfo</c>; <c>MessagePackCodec</c> registers
    /// <c>CollectionBaseFormatter&lt;CompanyCashRounding, CashRoundingItem&gt;</c>. Keyed lookup is
    /// provided by <see cref="FindUnit"/>.
    /// </remarks>
    [Description("Company cash-rounding override table.")]
    [MessagePackObject]
    public class CompanyCashRounding : MessagePackCollectionBase<CashRoundingItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompanyCashRounding"/>.
        /// </summary>
        public CompanyCashRounding()
        { }

        /// <summary>
        /// Finds the company cash-rounding unit for the specified currency, or <c>null</c> when the
        /// company defines no override for it. Matching is case-insensitive.
        /// </summary>
        /// <param name="currencyCode">The ISO 4217 alpha-3 currency code.</param>
        public decimal? FindUnit(string currencyCode)
        {
            if (string.IsNullOrEmpty(currencyCode)) { return null; }
            return this.FirstOrDefault(item =>
                string.Equals(item.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))?.Unit;
        }

        /// <summary>
        /// Gets the effective cash-rounding unit for the specified currency: the company override when
        /// present, otherwise the currency's natural minor unit from <paramref name="currencySettings"/>
        /// (which means no extra cash rounding beyond the natural decimals).
        /// </summary>
        /// <param name="currencyCode">The ISO 4217 alpha-3 currency code.</param>
        /// <param name="currencySettings">The system currency master used for the natural-unit fallback.</param>
        public decimal GetCashRounding(string currencyCode, CurrencySettings currencySettings)
        {
            ArgumentNullException.ThrowIfNull(currencySettings);
            return FindUnit(currencyCode) ?? currencySettings.GetRounding(currencyCode);
        }
    }
}
