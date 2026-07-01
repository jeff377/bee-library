using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Definition
{
    /// <summary>
    /// Carries the session/system inputs that <see cref="NumberFormatResolver"/> needs to resolve
    /// reference-aware decimal places and rounding: the current company (for company-level decimals,
    /// the default/home currency, and cash-rounding overrides) and the system currency master
    /// (for per-currency natural decimals). Both are optional — a resolver call with neither falls
    /// back to framework defaults. The unit-of-measure master is added by the uom increment.
    /// </summary>
    public sealed class RoundingContext
    {
        /// <summary>
        /// Gets the current company, or <c>null</c> when there is no company context (falls back to
        /// framework defaults for company-sourced kinds and to the framework currency fallback for
        /// amounts with no resolvable currency).
        /// </summary>
        public CompanyInfo? Company { get; init; }

        /// <summary>
        /// Gets the system currency master, or <c>null</c> when no currency master is deployed
        /// (amounts then fall back to framework-default decimals).
        /// </summary>
        public CurrencySettings? CurrencySettings { get; init; }

        /// <summary>
        /// Creates a context carrying only a company (no currency master). Convenience for
        /// company/system-fixed resolution where currency is not involved.
        /// </summary>
        /// <param name="company">The current company, or <c>null</c>.</param>
        public static RoundingContext ForCompany(CompanyInfo? company) => new() { Company = company };
    }
}
