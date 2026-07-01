namespace Bee.Definition
{
    /// <summary>
    /// The rounding strategy applied to a <see cref="NumberKind"/> when a value is written.
    /// </summary>
    public enum RoundingPolicy
    {
        /// <summary>
        /// Rounds half away from zero to the kind's resolved decimal places when the value is written.
        /// </summary>
        Round = 0,

        /// <summary>
        /// Preserves the input precision unchanged. Decimal places are display-only and are
        /// never written back, so source values do not inject rounding error downstream.
        /// </summary>
        Preserve,
    }
}
