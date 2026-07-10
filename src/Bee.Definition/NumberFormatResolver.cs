using Bee.Definition.Identity;

namespace Bee.Definition
{
    /// <summary>
    /// Resolves reference-aware decimal places, display format strings, and value rounding for a
    /// <see cref="NumberKind"/>. Amounts (<see cref="DecimalsSource.Currency"/>) resolve their decimals
    /// from the currency master by the reference currency code, and quantities/weights
    /// (<see cref="DecimalsSource.Unit"/>) resolve from the unit master by the reference unit code;
    /// company and system-fixed kinds resolve as in the core increment. A reference kind with no
    /// resolvable code (or no master deployed) falls back to the company decimals.
    /// </summary>
    public static class NumberFormatResolver
    {
        // ----- Reference-aware (multi-currency) API -----

        /// <summary>
        /// Resolves the decimal places for the kind. For <see cref="DecimalsSource.Currency"/> amounts,
        /// the decimals come from the currency master keyed by <paramref name="refCode"/> (the amount's
        /// current currency); an empty <paramref name="refCode"/> falls back to the company's default
        /// currency, then to framework defaults. System-fixed kinds always use the framework default;
        /// company and unit-fallback kinds use the company override table.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        /// <param name="ctx">The resolution context (company + currency master).</param>
        /// <param name="refCode">The reference currency code for amount fields; ignored for other kinds.</param>
        public static int ResolveDecimals(NumberKind kind, RoundingContext ctx, string? refCode = null)
        {
            ArgumentNullException.ThrowIfNull(ctx);
            var source = NumberKindProfile.GetDecimalsSource(kind);

            if (source == DecimalsSource.SystemFixed)
                return NumberKindProfile.GetDefaultDecimals(kind);

            if (source == DecimalsSource.Currency)
            {
                // Resolve the effective currency: the explicit reference code, else the company's
                // default (home) currency. When neither resolves — or no currency master is deployed —
                // fall back to the company table (Amount is normally absent there → framework default 2).
                string code = !string.IsNullOrEmpty(refCode) ? refCode! : (ctx.Company?.DefaultCurrency ?? string.Empty);
                if (ctx.CurrencySettings != null && !string.IsNullOrEmpty(code))
                    return ctx.CurrencySettings.GetDecimals(code);
                return ctx.Company?.GetDecimals(kind) ?? NumberKindProfile.GetDefaultDecimals(kind);
            }

            if (source == DecimalsSource.Unit)
            {
                // Quantities/weights resolve from the bound unit; with no unit code (or no unit master)
                // they fall back to the company decimals (else the framework default).
                if (ctx.UnitSettings != null && !string.IsNullOrEmpty(refCode))
                    return ctx.UnitSettings.GetDecimals(refCode!);
                return ctx.Company?.GetDecimals(kind) ?? NumberKindProfile.GetDefaultDecimals(kind);
            }

            // Company source.
            return ctx.Company?.GetDecimals(kind) ?? NumberKindProfile.GetDefaultDecimals(kind);
        }

        /// <summary>
        /// Resolves the decimal places for the kind using company/framework sources only (no currency
        /// reference). Amounts fall back to the company default currency when a currency master is set,
        /// otherwise to framework defaults.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        /// <param name="company">The current company, or <c>null</c> when there is no company context.</param>
        public static int ResolveDecimals(NumberKind kind, CompanyInfo? company)
        {
            return ResolveDecimals(kind, RoundingContext.ForCompany(company), null);
        }

        /// <summary>
        /// Resolves the .NET display format string for the kind (for example <c>"N2"</c> / <c>"P2"</c>),
        /// using <see cref="ResolveDecimals(NumberKind, RoundingContext, string?)"/>.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        /// <param name="ctx">The resolution context.</param>
        /// <param name="refCode">The reference currency code for amount fields; ignored for other kinds.</param>
        public static string ResolveFormat(NumberKind kind, RoundingContext ctx, string? refCode = null)
        {
            return NumberKindProfile.BuildFormatString(kind, ResolveDecimals(kind, ctx, refCode));
        }

        /// <summary>
        /// Resolves the .NET display format string for the kind using company/framework sources only.
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
        /// kinds round half away from zero to the reference-resolved decimal places. This is the
        /// per-detail rounding for the round-then-sum invariant — round each detail, then sum the
        /// already-rounded values so the total equals the sum of details. Never sum at full precision
        /// and round once at the end.
        /// </summary>
        /// <param name="value">The value to round.</param>
        /// <param name="kind">The number kind.</param>
        /// <param name="ctx">The resolution context.</param>
        /// <param name="refCode">The reference currency code for amount fields; ignored for other kinds.</param>
        public static decimal RoundByKind(decimal value, NumberKind kind, RoundingContext ctx, string? refCode = null)
        {
            if (NumberKindProfile.GetRoundingPolicy(kind) == RoundingPolicy.Preserve)
                return value;

            return Math.Round(value, ResolveDecimals(kind, ctx, refCode), MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Rounds a value using company/framework sources only (no currency reference). See
        /// <see cref="RoundByKind(decimal, NumberKind, RoundingContext, string?)"/> for the policy.
        /// </summary>
        /// <param name="value">The value to round.</param>
        /// <param name="kind">The number kind.</param>
        /// <param name="company">The current company, or <c>null</c> when there is no company context.</param>
        public static decimal RoundByKind(decimal value, NumberKind kind, CompanyInfo? company)
        {
            return RoundByKind(value, kind, RoundingContext.ForCompany(company), null);
        }

        /// <summary>
        /// Rounds a document's final payable amount to its currency's cash-rounding unit (SAP T001R-style):
        /// the company override for the currency when present, otherwise the currency's natural minor unit
        /// (which means no extra rounding). Rounds to the nearest multiple of that unit, half away from zero.
        /// The caller records the deliberate difference (<c>payable − total</c>) against a rounding account.
        /// </summary>
        /// <remarks>
        /// This is the final layer, distinct from the per-detail <see cref="RoundByKind(decimal, NumberKind, RoundingContext, string?)"/>:
        /// details are rounded to the currency's natural decimals and summed (round-then-sum); only the
        /// final payable is optionally snapped to the cash-rounding unit.
        /// </remarks>
        /// <param name="total">The summed total (already at the currency's natural decimals).</param>
        /// <param name="currencyCode">The document currency code.</param>
        /// <param name="ctx">The resolution context (company + currency master).</param>
        public static decimal RoundCash(decimal total, string currencyCode, RoundingContext ctx)
        {
            ArgumentNullException.ThrowIfNull(ctx);

            decimal unit = ResolveCashUnit(currencyCode, ctx);
            if (unit <= 0m)
                return total;

            return Math.Round(total / unit, 0, MidpointRounding.AwayFromZero) * unit;
        }

        /// <summary>
        /// Resolves the effective cash-rounding unit: the company override when present, otherwise the
        /// currency's natural minor unit from the currency master (or the framework fallback when no
        /// master is deployed).
        /// </summary>
        private static decimal ResolveCashUnit(string currencyCode, RoundingContext ctx)
        {
            if (ctx.Company != null && ctx.CurrencySettings != null)
                return ctx.Company.GetCashRounding(currencyCode, ctx.CurrencySettings);
            return ctx.CurrencySettings?.GetRounding(currencyCode) ?? Settings.CurrencySettings.FallbackRounding;
        }
    }
}
