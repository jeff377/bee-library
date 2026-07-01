using System.Globalization;

namespace Bee.Definition
{
    /// <summary>
    /// Resolves the framework defaults for each <see cref="NumberKind"/>: format letter,
    /// default decimal places, rounding policy, and decimals source. Replaces the former
    /// string-keyed <c>NumberFormatPresets</c>. The returned values are the signed-off
    /// contract in plan-numeric-core.md.
    /// </summary>
    public static class NumberKindProfile
    {
        /// <summary>
        /// Gets the .NET numeric format letter for the specified kind (<c>'P'</c> for
        /// <see cref="NumberKind.Percent"/>, otherwise <c>'N'</c>).
        /// </summary>
        /// <param name="kind">The number kind.</param>
        public static char GetFormatLetter(NumberKind kind)
        {
            return kind == NumberKind.Percent ? 'P' : 'N';
        }

        /// <summary>
        /// Gets the framework default decimal places for the specified kind. System-fixed
        /// kinds (<see cref="NumberKind.ExchangeRate"/>) use this value directly; company and
        /// reference-bound kinds use it only as the fallback when no override resolves.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        public static int GetDefaultDecimals(NumberKind kind)
        {
            return kind switch
            {
                NumberKind.Quantity => 0,
                NumberKind.Weight => 3,
                NumberKind.Amount => 2,
                NumberKind.Percent => 2,
                NumberKind.UnitPrice => 4,
                NumberKind.Cost => 4,
                NumberKind.ExchangeRate => 5,
                _ => 0,
            };
        }

        /// <summary>
        /// Gets the rounding policy for the specified kind. <see cref="NumberKind.Quantity"/>,
        /// <see cref="NumberKind.Weight"/>, <see cref="NumberKind.Amount"/>, and
        /// <see cref="NumberKind.Percent"/> round; <see cref="NumberKind.UnitPrice"/>,
        /// <see cref="NumberKind.Cost"/>, and <see cref="NumberKind.ExchangeRate"/> are preserved.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        public static RoundingPolicy GetRoundingPolicy(NumberKind kind)
        {
            return kind switch
            {
                NumberKind.Quantity or NumberKind.Weight or NumberKind.Amount or NumberKind.Percent
                    => RoundingPolicy.Round,
                _ => RoundingPolicy.Preserve,
            };
        }

        /// <summary>
        /// Gets the decimals source for the specified kind. <see cref="NumberKind.Amount"/> binds
        /// to currency, <see cref="NumberKind.Quantity"/>/<see cref="NumberKind.Weight"/> bind to
        /// unit, <see cref="NumberKind.ExchangeRate"/> is system-fixed, and the rest resolve from
        /// the company override table.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        public static DecimalsSource GetDecimalsSource(NumberKind kind)
        {
            return kind switch
            {
                NumberKind.Amount => DecimalsSource.Currency,
                NumberKind.Quantity or NumberKind.Weight => DecimalsSource.Unit,
                NumberKind.ExchangeRate => DecimalsSource.SystemFixed,
                _ => DecimalsSource.Company,
            };
        }

        /// <summary>
        /// Builds the .NET format string from a kind and decimal places, for example
        /// <c>(Quantity, 3)</c> produces <c>"N3"</c> and <c>(Percent, 2)</c> produces <c>"P2"</c>.
        /// </summary>
        /// <param name="kind">The number kind (selects the format letter).</param>
        /// <param name="decimals">The decimal places.</param>
        public static string BuildFormatString(NumberKind kind, int decimals)
        {
            return $"{GetFormatLetter(kind)}{decimals.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}
