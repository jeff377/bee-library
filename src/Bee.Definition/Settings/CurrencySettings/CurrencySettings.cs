using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// System-level currency master (SAP TCURX-style): a curated table of <see cref="CurrencyItem"/>
    /// carrying each currency's natural minor unit. Currency decimals are system-wide and independent
    /// of company; the company layer may only override the final cash-rounding unit
    /// (<c>CompanyCashRounding</c>). Persisted as a singleton define through <c>IDefineStorage</c>
    /// (file or DB) and shipped to the client so the UI can resolve amount decimals at runtime.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="MessagePackCollectionBase{T}"/> (not a keyed collection) so the table travels
    /// over the MessagePack wire cleanly; the custom MessagePack resolver only recognises this base,
    /// and <c>MessagePackCodec</c> registers <c>CollectionBaseFormatter&lt;CurrencySettings, CurrencyItem&gt;</c>.
    /// Keyed-lookup semantics are provided by the <see cref="Find"/> family.
    /// </remarks>
    [Description("System-level currency master.")]
    [MessagePackObject]
    [XmlRoot("CurrencySettings")]
    public class CurrencySettings : MessagePackCollectionBase<CurrencyItem>
    {
        /// <summary>The fallback rounding factor used when a currency code is not found (two decimals).</summary>
        public const decimal FallbackRounding = 0.01m;

        /// <summary>
        /// Initializes a new instance of <see cref="CurrencySettings"/>.
        /// </summary>
        public CurrencySettings()
        { }

        /// <summary>
        /// Finds the currency item for the specified code, or <c>null</c> when the code is not defined.
        /// Matching is case-insensitive (ISO codes are upper-case by convention).
        /// </summary>
        /// <param name="code">The ISO 4217 alpha-3 currency code.</param>
        public CurrencyItem? Find(string code)
        {
            if (string.IsNullOrEmpty(code)) { return null; }
            return this.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the natural minor unit (rounding factor) for the specified currency code, or
        /// <see cref="FallbackRounding"/> (<c>0.01</c>) when the code is not defined.
        /// </summary>
        /// <param name="code">The ISO 4217 alpha-3 currency code.</param>
        public decimal GetRounding(string code)
        {
            return Find(code)?.Rounding ?? FallbackRounding;
        }

        /// <summary>
        /// Gets the display decimal places for the specified currency code, derived from its rounding
        /// factor: <c>0.01</c> yields <c>2</c>, <c>1</c> yields <c>0</c>. Falls back to two decimals
        /// when the code is not defined.
        /// </summary>
        /// <param name="code">The ISO 4217 alpha-3 currency code.</param>
        public int GetDecimals(string code)
        {
            return DecimalsFromRounding(GetRounding(code));
        }

        /// <summary>
        /// Derives display decimal places from a rounding factor: counts how many powers of ten scale
        /// the factor up to at least one (<c>0.01</c> → 2, <c>0.001</c> → 3, <c>1</c> → 0). Uses a
        /// decimal-safe loop rather than <c>log10</c> to avoid floating-point rounding at the boundary.
        /// </summary>
        /// <param name="rounding">The rounding factor (natural minor unit).</param>
        public static int DecimalsFromRounding(decimal rounding)
        {
            if (rounding <= 0m || rounding >= 1m) { return 0; }
            int decimals = 0;
            decimal value = rounding;
            while (value < 1m)
            {
                value *= 10m;
                decimals++;
            }
            return decimals;
        }
    }
}
