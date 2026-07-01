namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Decides whether an amount column's footer total is meaningful. Summing across different
    /// currencies has no meaning (USD + JPY), so an original-currency column shows a total only when
    /// every populated row shares one currency; a home-currency column is always one currency and
    /// therefore always totalable (see plan-numeric-multicurrency.md §3.2c, mirroring SAP ALV
    /// <c>DO_SUM</c>). This is display-only logic — it does not round; callers format the returned sum
    /// with the resolved currency's decimals.
    /// </summary>
    public static class AmountColumnSummary
    {
        /// <summary>
        /// Computes the column total when it is meaningful: the sum of all cell values when every cell
        /// with a non-empty currency code shares the same currency; <c>null</c> when the column mixes
        /// currencies (the footer then shows no total). Cells with an empty currency code are summed but
        /// do not by themselves establish or break the single-currency condition.
        /// </summary>
        /// <param name="cells">The column's (value, currencyCode) pairs across the visible rows.</param>
        /// <returns>The total when single-currency (or all-empty); otherwise <c>null</c> for mixed currencies.</returns>
        public static decimal? TryComputeTotal(IEnumerable<(decimal Value, string CurrencyCode)> cells)
        {
            ArgumentNullException.ThrowIfNull(cells);

            string? currency = null;
            decimal sum = 0m;
            foreach (var (value, code) in cells)
            {
                sum += value;
                if (string.IsNullOrEmpty(code)) { continue; }
                if (currency is null)
                {
                    currency = code;
                }
                else if (!string.Equals(currency, code, StringComparison.OrdinalIgnoreCase))
                {
                    // Mixed currencies — no meaningful total.
                    return null;
                }
            }
            return sum;
        }
    }
}
