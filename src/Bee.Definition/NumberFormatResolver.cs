using Bee.Definition.Identity;

namespace Bee.Definition
{
    /// <summary>
    /// Resolves company-aware decimal places, display format strings, and value rounding for a
    /// <see cref="NumberKind"/>. This is the core-increment resolver: it has no currency or unit
    /// settings, so <see cref="DecimalsSource.Currency"/> and <see cref="DecimalsSource.Unit"/>
    /// fall back to the company override table (else the framework default). The multi-currency and
    /// unit-of-measure increments replace those fallbacks with real reference-field resolution.
    /// </summary>
    public static class NumberFormatResolver
    {
        /// <summary>
        /// Resolves the decimal places for the kind: the framework default for
        /// <see cref="DecimalsSource.SystemFixed"/> kinds (and whenever <paramref name="company"/> is
        /// <c>null</c>), otherwise the company override table with the framework default as fallback.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        /// <param name="company">The current company, or <c>null</c> when there is no company context.</param>
        public static int ResolveDecimals(NumberKind kind, CompanyInfo? company)
        {
            var source = NumberKindProfile.GetDecimalsSource(kind);
            if (source == DecimalsSource.SystemFixed || company == null)
                return NumberKindProfile.GetDefaultDecimals(kind);

            // Company, plus Currency/Unit which fall back to the company table in the core increment.
            return company.GetDecimals(kind);
        }

        /// <summary>
        /// Resolves the .NET display format string for the kind (for example <c>"N2"</c> / <c>"P2"</c>),
        /// using <see cref="ResolveDecimals"/>.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        /// <param name="company">The current company, or <c>null</c> when there is no company context.</param>
        public static string ResolveFormat(NumberKind kind, CompanyInfo? company)
        {
            return NumberKindProfile.BuildFormatString(kind, ResolveDecimals(kind, company));
        }

        /// <summary>
        /// Rounds a value according to the kind's rounding policy: <see cref="RoundingPolicy.Preserve"/>
        /// kinds (unit price, cost, exchange rate) return the value unchanged; <see cref="RoundingPolicy.Round"/>
        /// kinds round half away from zero to the resolved decimal places.
        /// </summary>
        /// <remarks>
        /// This is the per-detail rounding used to build the round-then-sum invariant: round each detail
        /// with this method, then sum the already-rounded values so the total equals the sum of details.
        /// Never sum at full precision and round once at the end.
        /// </remarks>
        /// <param name="value">The value to round.</param>
        /// <param name="kind">The number kind.</param>
        /// <param name="company">The current company, or <c>null</c> when there is no company context.</param>
        public static decimal RoundByKind(decimal value, NumberKind kind, CompanyInfo? company)
        {
            if (NumberKindProfile.GetRoundingPolicy(kind) == RoundingPolicy.Preserve)
                return value;

            return Math.Round(value, ResolveDecimals(kind, company), MidpointRounding.AwayFromZero);
        }
    }
}
