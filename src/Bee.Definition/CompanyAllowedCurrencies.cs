using Bee.Base.Collections;
using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.Definition
{
    /// <summary>
    /// A company-level whitelist of usable currency codes (a per-company subset stricter than the
    /// SAP/Odoo global model). Empty means the company may use every currency in the system
    /// <see cref="CurrencySettings"/>. Drives the currency drop-down options on documents. Carried by
    /// <see cref="Bee.Definition.Identity.CompanyInfo"/> over the MessagePack wire.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="CollectionBase{T}"/> so the whitelist serializes cleanly as part of
    /// <see cref="Bee.Definition.Identity.CompanyInfo"/>; <c>MessagePackCodec</c> registers
    /// <c>CollectionBaseFormatter&lt;CompanyAllowedCurrencies, AllowedCurrencyItem&gt;</c>.
    /// </remarks>
    [Description("Company allowed-currency whitelist.")]
    public class CompanyAllowedCurrencies : CollectionBase<AllowedCurrencyItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompanyAllowedCurrencies"/>.
        /// </summary>
        public CompanyAllowedCurrencies()
        { }

        /// <summary>
        /// Gets the effective list of usable currency codes: this whitelist when it is non-empty,
        /// otherwise every code defined in <paramref name="currencySettings"/>.
        /// </summary>
        /// <param name="currencySettings">The system currency master used when the whitelist is empty.</param>
        public IReadOnlyList<string> GetAllowedCurrencies(CurrencySettings currencySettings)
        {
            ArgumentNullException.ThrowIfNull(currencySettings);
            if (this.Count > 0)
                return this.Select(item => item.Code).ToArray();
            return currencySettings.Select(item => item.Code).ToArray();
        }
    }
}
