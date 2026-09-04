using Bee.Base;
using Bee.Definition.Settings;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// Metadata describing a company that a user may enter for a session.
    /// </summary>
    /// <remarks>
    /// Returned by <c>EnterCompany</c> and resolved by company-aware repositories
    /// via the cached <see cref="ICompanyInfoService"/>. <c>CompanyDatabaseId</c> references
    /// a logical <see cref="DatabaseSettings"/> entry; multiple companies may point at the
    /// same id and rely on the <c>sys_company_rowid</c> column for row-level
    /// isolation. The log database is shared across all companies under a fixed
    /// <c>"log"</c> databaseId (see <see cref="DbScope.Log"/>), so there is no per-company
    /// log database id property.
    /// <para>
    /// WARNING: this is a cache-shared instance. It must not be mutated after it is loaded — every
    /// session in the company receives the same reference, and <see cref="CompanyDatabaseId"/>
    /// selects which database that company's repositories read and write, so a mutation redirects
    /// other sessions' data access. The setters exist for the serializers, not for callers. See
    /// <c>docs/development-constraints.md</c> § <i>Cached Data Immutability After Init</i>.
    /// </para>
    /// </remarks>
    public class CompanyInfo : IKeyObject
    {
        #region IKeyObject Interface

        /// <summary>
        /// Gets the item key value (the company id).
        /// </summary>
        public string GetKey()
        {
            return this.CompanyId;
        }

        #endregion

        /// <summary>
        /// Gets or sets the company id (unique key).
        /// </summary>
        public string CompanyId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company display name.
        /// </summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the logical <see cref="DatabaseSettings"/> id used for the
        /// company-category database during this session.
        /// </summary>
        public string CompanyDatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tenant customization code for this company.
        /// </summary>
        /// <remarks>
        /// Empty means the standard (non-customized) deployment. Companies map many-to-one onto a
        /// customization code (a group can share one customization set). Loaded from the
        /// <c>customize_id</c> column by <c>CompanyRepository</c>; <c>EnterCompany</c> copies it
        /// into <see cref="SessionInfo.CustomizeId"/> for the session's customization overlay.
        /// </remarks>
        public string CustomizeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company-level decimal-places override table. Empty means every kind uses
        /// the framework default. Loaded from the <c>number_formats_xml</c> column by
        /// <c>CompanyRepository</c>; carries Percent and UnitPrice/Cost display decimals plus the
        /// Quantity/Weight fallback when no unit is bound.
        /// </summary>
        public CompanyNumberFormats NumberFormats { get; set; } = [];

        /// <summary>
        /// Gets the decimal places for the specified kind: the company override when present,
        /// otherwise the framework default from <see cref="NumberKindProfile.GetDefaultDecimals"/>.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        public int GetDecimals(NumberKind kind)
        {
            return NumberFormats.FindDecimals(kind) ?? NumberKindProfile.GetDefaultDecimals(kind);
        }

        /// <summary>
        /// Gets or sets the company's default (local/home) currency code — an ISO 4217 alpha-3 code
        /// matching a <see cref="CurrencySettings"/> entry. Empty means unset (amounts with no resolvable
        /// currency fall back to the framework default of two decimals). Loaded from the
        /// <c>default_currency</c> column by <c>CompanyRepository</c>.
        /// </summary>
        public string DefaultCurrency { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company-level cash-rounding override table (SAP T001R-style). Empty means
        /// no extra cash rounding — final amounts stay at each currency's natural minor unit. Loaded
        /// from the <c>cash_rounding_xml</c> column by <c>CompanyRepository</c>.
        /// </summary>
        public CompanyCashRounding CashRounding { get; set; } = [];

        /// <summary>
        /// Gets or sets the company's allowed-currency whitelist. Empty means every system currency is
        /// usable. Drives the currency drop-down options on documents. Loaded from the
        /// <c>allowed_currencies_xml</c> column by <c>CompanyRepository</c>.
        /// </summary>
        public CompanyAllowedCurrencies AllowedCurrencies { get; set; } = [];

        /// <summary>
        /// Gets the effective cash-rounding unit for the specified currency: the company override when
        /// present, otherwise the currency's natural minor unit from <paramref name="currencySettings"/>.
        /// </summary>
        /// <param name="currencyCode">The ISO 4217 alpha-3 currency code.</param>
        /// <param name="currencySettings">The system currency master used for the natural-unit fallback.</param>
        public decimal GetCashRounding(string currencyCode, CurrencySettings currencySettings)
        {
            return CashRounding.GetCashRounding(currencyCode, currencySettings);
        }

        /// <summary>
        /// Gets the effective list of usable currency codes: the company whitelist when non-empty,
        /// otherwise every code defined in <paramref name="currencySettings"/>.
        /// </summary>
        /// <param name="currencySettings">The system currency master used when the whitelist is empty.</param>
        public IReadOnlyList<string> GetAllowedCurrencies(CurrencySettings currencySettings)
        {
            return AllowedCurrencies.GetAllowedCurrencies(currencySettings);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{CompanyId} : {CompanyName}";
        }
    }
}
