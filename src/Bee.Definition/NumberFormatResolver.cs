using Bee.Definition.Identity;

namespace Bee.Definition
{
    /// <summary>
    /// Resolves reference-aware decimal places, display format strings, and value rounding for a
    /// <see cref="NumberKind"/>. Amounts (<see cref="DecimalsSource.Currency"/>) resolve their decimals
    /// from the currency master by the reference currency code, and quantities/weights
    /// (<see cref="DecimalsSource.Unit"/>) resolve from the unit master by the reference unit code;
    /// company and system-fixed kinds resolve as in the core increment. An amount with no currency
    /// master deployed falls back to the company decimals; a quantity or weight never uses the company.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An amount with no reference currency resolves by the company's
    /// <see cref="CompanyInfo.DefaultCurrency"/>, which is required: when a company is present but
    /// carries no default currency, the resolving members throw <see cref="InvalidOperationException"/>
    /// instead of falling back. Only a call with no company context falls back to framework defaults.
    /// </para>
    /// <para>
    /// A quantity or weight takes its decimals only from its unit, because a company has a home currency
    /// but no default unit. A unit code that is not in the unit master, or a missing master, resolves to
    /// the framework default for the kind. An empty unit code also resolves to that default for display,
    /// but <see cref="RoundByKind(decimal, NumberKind, RoundingContext, string?)"/> returns the value
    /// unchanged instead of rounding it to a decimal count that no unit chose.
    /// </para>
    /// </remarks>
    public static class NumberFormatResolver
    {
        // ----- Reference-aware (multi-currency) API -----

        /// <summary>
        /// Resolves the decimal places for the kind. For <see cref="DecimalsSource.Currency"/> amounts,
        /// the decimals come from the currency master keyed by <paramref name="refCode"/> (the amount's
        /// current currency); an empty <paramref name="refCode"/> falls back to the company's default
        /// currency, and only with no company context to framework defaults. For
        /// <see cref="DecimalsSource.Unit"/> quantities and weights, the decimals come from the unit master
        /// keyed by <paramref name="refCode"/>; an empty or unknown code, or a missing master, yields the
        /// framework default. System-fixed kinds always use the framework default; company kinds use the
        /// company override table.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        /// <param name="ctx">The resolution context (company, currency master, and unit master).</param>
        /// <param name="refCode">
        /// The reference code: the currency code for amounts, the unit code for quantities and weights;
        /// ignored for other kinds.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="kind"/> is an amount, <paramref name="refCode"/> is empty, and the company in
        /// <paramref name="ctx"/> has no default currency.
        /// </exception>
        public static int ResolveDecimals(NumberKind kind, RoundingContext ctx, string? refCode = null)
        {
            ArgumentNullException.ThrowIfNull(ctx);
            var source = NumberKindProfile.GetDecimalsSource(kind);

            if (source == DecimalsSource.SystemFixed)
                return NumberKindProfile.GetDefaultDecimals(kind);

            if (source == DecimalsSource.Currency)
            {
                // Resolve the effective currency: the explicit reference code, else the company's
                // default (home) currency. With no company context — or no currency master deployed —
                // fall back to the company table (Amount is normally absent there → framework default 2).
                string code = !string.IsNullOrEmpty(refCode) ? refCode : ResolveCompanyCurrency(ctx.Company);
                if (ctx.CurrencySettings != null && !string.IsNullOrEmpty(code))
                    return ctx.CurrencySettings.GetDecimals(code);
                return ctx.Company?.GetDecimals(kind) ?? NumberKindProfile.GetDefaultDecimals(kind);
            }

            if (source == DecimalsSource.Unit)
            {
                // Never the company: a company has a home currency to fall back to, but no default unit.
                // An empty or unknown code, or no unit master, leaves only the kind's framework default.
                var unit = string.IsNullOrEmpty(refCode) ? null : ctx.UnitSettings?.Find(refCode);
                return unit?.Decimals ?? NumberKindProfile.GetDefaultDecimals(kind);
            }

            // Company source.
            return ctx.Company?.GetDecimals(kind) ?? NumberKindProfile.GetDefaultDecimals(kind);
        }

        /// <summary>
        /// Resolves the decimal places for the kind using company/framework sources only (no reference
        /// code). Amounts fall back to the company default currency when a currency master is set,
        /// otherwise to framework defaults. Quantities and weights have no unit code here, so they use the
        /// framework default.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        /// <param name="company">The current company, or <c>null</c> when there is no company context.</param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="kind"/> is an amount and <paramref name="company"/> has no default currency.
        /// </exception>
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
        /// <param name="refCode">
        /// The reference code: the currency code for amounts, the unit code for quantities and weights;
        /// ignored for other kinds.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="kind"/> is an amount, <paramref name="refCode"/> is empty, and the company in
        /// <paramref name="ctx"/> has no default currency.
        /// </exception>
        public static string ResolveFormat(NumberKind kind, RoundingContext ctx, string? refCode = null)
        {
            return NumberKindProfile.BuildFormatString(kind, ResolveDecimals(kind, ctx, refCode));
        }

        /// <summary>
        /// Resolves the .NET display format string for the kind using company/framework sources only.
        /// Quantities and weights have no unit code here, so they use the framework default.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        /// <param name="company">The current company, or <c>null</c> when there is no company context.</param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="kind"/> is an amount and <paramref name="company"/> has no default currency.
        /// </exception>
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
        /// <remarks>
        /// A quantity or weight with an empty <paramref name="refCode"/> is returned unchanged: its unit is
        /// not known yet, and rounding it would discard digits that the eventual unit may keep.
        /// </remarks>
        /// <param name="value">The value to round.</param>
        /// <param name="kind">The number kind.</param>
        /// <param name="ctx">The resolution context.</param>
        /// <param name="refCode">
        /// The reference code: the currency code for amounts, the unit code for quantities and weights;
        /// ignored for other kinds.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="kind"/> is an amount, <paramref name="refCode"/> is empty, and the company in
        /// <paramref name="ctx"/> has no default currency.
        /// </exception>
        public static decimal RoundByKind(decimal value, NumberKind kind, RoundingContext ctx, string? refCode = null)
        {
            if (NumberKindProfile.GetRoundingPolicy(kind) == RoundingPolicy.Preserve)
                return value;

            // No unit yet (for example an empty unit cell on a new line): keep full precision.
            if (NumberKindProfile.GetDecimalsSource(kind) == DecimalsSource.Unit && string.IsNullOrEmpty(refCode))
                return value;

            return Math.Round(value, ResolveDecimals(kind, ctx, refCode), MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Rounds a value using company/framework sources only (no reference code). See
        /// <see cref="RoundByKind(decimal, NumberKind, RoundingContext, string?)"/> for the policy; a
        /// quantity or weight is returned unchanged because there is no unit code.
        /// </summary>
        /// <param name="value">The value to round.</param>
        /// <param name="kind">The number kind.</param>
        /// <param name="company">The current company, or <c>null</c> when there is no company context.</param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="kind"/> is an amount and <paramref name="company"/> has no default currency.
        /// </exception>
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

        /// <summary>
        /// Gets the company's default currency for an amount with no reference currency, or an empty
        /// string when there is no company context.
        /// </summary>
        /// <param name="company">The current company, or <c>null</c>.</param>
        /// <exception cref="InvalidOperationException">The company has no default currency.</exception>
        private static string ResolveCompanyCurrency(CompanyInfo? company)
        {
            if (company == null) { return string.Empty; }

            // IMPORTANT: a company without a default currency is a configuration error, not a reason to
            // fall back. Falling back would round its amounts to decimals that no currency of that
            // company chose, and nothing downstream would notice.
            if (string.IsNullOrWhiteSpace(company.DefaultCurrency))
                throw new InvalidOperationException($"Company '{company.CompanyId}' has no default currency configured.");

            return company.DefaultCurrency;
        }
    }
}
