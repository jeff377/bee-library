using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Definition
{
    /// <summary>
    /// Carries the session/system inputs that <see cref="NumberFormatResolver"/> needs to resolve
    /// reference-aware decimal places and rounding: the current company (for company-level decimals,
    /// the default/home currency, and cash-rounding overrides), the system currency master
    /// (for per-currency natural decimals), and the system unit-of-measure master (for per-unit
    /// decimals). All are optional — a resolver call with none falls back to framework defaults.
    /// </summary>
    public sealed class RoundingContext
    {
        /// <summary>
        /// Gets the current company, or <c>null</c> when there is no company context (falls back to
        /// framework defaults for company-sourced kinds and to the framework currency fallback for
        /// amounts with no resolvable currency).
        /// </summary>
        /// <remarks>
        /// A company supplied here must carry a <see cref="CompanyInfo.DefaultCurrency"/>:
        /// <see cref="NumberFormatResolver"/> throws when it resolves an amount with no reference currency
        /// against a company without one. The framework-default fallback applies only when this is <c>null</c>.
        /// </remarks>
        public CompanyInfo? Company { get; init; }

        /// <summary>
        /// Gets the system currency master, or <c>null</c> when no currency master is deployed
        /// (amounts then fall back to framework-default decimals).
        /// </summary>
        public CurrencySettings? CurrencySettings { get; init; }

        /// <summary>
        /// Gets the system unit-of-measure master, or <c>null</c> when no unit master is deployed
        /// (quantities/weights then use the framework default for their kind; they never use the company
        /// decimals).
        /// </summary>
        public UnitSettings? UnitSettings { get; init; }

        /// <summary>
        /// Creates a context carrying only a company (no currency master). Convenience for
        /// company/system-fixed resolution where currency is not involved.
        /// </summary>
        /// <param name="company">The current company, or <c>null</c>.</param>
        public static RoundingContext ForCompany(CompanyInfo? company) => new() { Company = company };
    }
}
