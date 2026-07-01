namespace Bee.Definition
{
    /// <summary>
    /// Semantic classification of a numeric field. Drives the field's rounding
    /// policy (<see cref="RoundingPolicy"/>), decimal-places source
    /// (<see cref="DecimalsSource"/>), and display format. The members and their
    /// framework defaults are the signed-off contract in plan-numeric-core.md.
    /// </summary>
    public enum NumberKind
    {
        /// <summary>Non-semantic or non-numeric field; no numeric handling is applied.</summary>
        None = 0,

        /// <summary>Quantity. Rounds to unit-bound decimals (falls back to company); framework default 0.</summary>
        Quantity,

        /// <summary>Weight. Rounds to unit-bound decimals (falls back to company); framework default 3.</summary>
        Weight,

        /// <summary>Monetary amount, tax, or total. Rounds to currency-bound decimals (falls back to company); framework default 2.</summary>
        Amount,

        /// <summary>Percentage. Rounds to company decimals; framework default 2.</summary>
        Percent,

        /// <summary>Unit price. Preserved at input precision; decimals are display-only; framework default 4.</summary>
        UnitPrice,

        /// <summary>Cost. Preserved at input precision; decimals are display-only; framework default 4.</summary>
        Cost,

        /// <summary>Exchange rate. Preserved at input precision; system-fixed display decimals; framework default 5.</summary>
        ExchangeRate,
    }
}
