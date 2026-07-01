using Bee.Base;
using Bee.Definition.Settings;
using MessagePack;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// Metadata describing a company that a user may enter for a session.
    /// </summary>
    /// <remarks>
    /// Returned by <c>EnterCompany</c> and resolved by company-aware repositories
    /// via the cached <c>ICompanyInfoService</c>. <c>CompanyDatabaseId</c> references
    /// a logical <c>DatabaseSettings</c> entry; multiple companies may point at the
    /// same id and rely on the <c>sys_company_rowid</c> column for row-level
    /// isolation. The log database is shared across all companies under a fixed
    /// <c>"log"</c> databaseId (see <c>DbScope.Log</c> in plan-bo-repo-db-routing),
    /// so there is no per-company log database id property.
    /// </remarks>
    [MessagePackObject]
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
        [Key(0)]
        public string CompanyId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company display name.
        /// </summary>
        [Key(1)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the logical <c>DatabaseSettings</c> id used for the
        /// company-category database during this session.
        /// </summary>
        [Key(2)]
        public string CompanyDatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tenant customization code for this company.
        /// </summary>
        /// <remarks>
        /// Empty means the standard (non-customized) deployment. Companies map many-to-one onto a
        /// customization code (a group can share one customization set). Loaded from the
        /// <c>customize_id</c> column by <c>CompanyRepository</c>; <c>EnterCompany</c> copies it
        /// into <c>SessionInfo.CustomizeId</c> for the session's customization overlay.
        /// </remarks>
        [Key(3)]
        public string CustomizeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company-level decimal-places override table. Empty means every kind uses
        /// the framework default. Loaded from the <c>number_formats_xml</c> column by
        /// <c>CompanyRepository</c>; carries Percent and UnitPrice/Cost display decimals plus the
        /// Quantity/Weight fallback when no unit is bound (see plan-numeric-core.md).
        /// </summary>
        [Key(4)]
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
        /// matching a <c>CurrencySettings</c> entry. Empty means unset (amounts with no resolvable
        /// currency fall back to the framework default of two decimals). Loaded from the
        /// <c>default_currency</c> column by <c>CompanyRepository</c>.
        /// </summary>
        [Key(5)]
        public string DefaultCurrency { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company-level cash-rounding override table (SAP T001R-style). Empty means
        /// no extra cash rounding — final amounts stay at each currency's natural minor unit. Loaded
        /// from the <c>cash_rounding_xml</c> column by <c>CompanyRepository</c>.
        /// </summary>
        [Key(6)]
        public CompanyCashRounding CashRounding { get; set; } = [];

        /// <summary>
        /// Gets or sets the company's allowed-currency whitelist. Empty means every system currency is
        /// usable. Drives the currency drop-down options on documents. Loaded from the
        /// <c>allowed_currencies_xml</c> column by <c>CompanyRepository</c>.
        /// </summary>
        [Key(7)]
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
