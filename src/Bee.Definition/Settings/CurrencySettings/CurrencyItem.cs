using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A single system-level currency definition (SAP TCURX-style), held in
    /// <see cref="CurrencySettings"/>. Currency decimals are system-wide (curated master data,
    /// independent of company); the company layer may only override the final cash-rounding unit.
    /// </summary>
    [Description("System-level currency definition item.")]
    [MessagePackObject]
    public sealed class CurrencyItem : MessagePackCollectionItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CurrencyItem"/>.
        /// </summary>
        public CurrencyItem()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="CurrencyItem"/>.
        /// </summary>
        /// <param name="code">The ISO 4217 alpha-3 currency code (the key).</param>
        /// <param name="rounding">The natural minor unit (for example <c>0.01</c> or <c>1</c>).</param>
        /// <param name="symbol">The display symbol.</param>
        /// <param name="name">The display name.</param>
        /// <param name="numeric">The ISO 4217 numeric code (optional).</param>
        public CurrencyItem(string code, decimal rounding, string symbol = "", string name = "", string numeric = "")
        {
            Code = code;
            Rounding = rounding;
            Symbol = symbol;
            Name = name;
            Numeric = numeric;
        }

        /// <summary>
        /// Gets or sets the ISO 4217 alpha-3 currency code (for example <c>USD</c>, <c>JPY</c>,
        /// <c>BHD</c>). This is the lookup key.
        /// </summary>
        [XmlAttribute]
        [Key(100)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ISO 4217 numeric code (for example <c>840</c>, <c>392</c>). Optional;
        /// stored for reference and interchange.
        /// </summary>
        [XmlAttribute]
        [Key(101)]
        public string Numeric { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the natural minor unit (ISO 4217 minor unit) for this currency: <c>0.01</c>
        /// for two-decimal currencies, <c>1</c> for zero-decimal currencies (JPY). This expresses the
        /// currency's inherent smallest unit and drives display decimals; it does not carry any
        /// company cash-rounding policy (see <c>CompanyCashRounding</c>).
        /// </summary>
        [XmlAttribute]
        [Key(102)]
        public decimal Rounding { get; set; } = 0.01m;

        /// <summary>
        /// Gets or sets the display symbol (for example <c>$</c>, <c>¥</c>).
        /// </summary>
        [XmlAttribute]
        [Key(103)]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name (for example <c>US Dollar</c>).
        /// </summary>
        [XmlAttribute]
        [Key(104)]
        public string Name { get; set; } = string.Empty;
    }
}
